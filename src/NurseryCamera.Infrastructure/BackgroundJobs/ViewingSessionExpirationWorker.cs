using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NurseryCamera.Application.Abstractions.Audit;
using NurseryCamera.Application.Abstractions.Streaming;
using NurseryCamera.Application.Abstractions.Time;
using NurseryCamera.Application.Common.Options;
using NurseryCamera.Domain.Entities;
using NurseryCamera.Domain.Enums;
using NurseryCamera.Infrastructure.Persistence;

namespace NurseryCamera.Infrastructure.BackgroundJobs;

/// <summary>
/// Spec section 20: every few seconds, finds ACTIVE viewing sessions whose ExpiresAtUtc
/// has passed, marks them EXPIRED, revokes their stream tokens, notifies the media
/// gateway, and writes an audit trail.
/// </summary>
public sealed class ViewingSessionExpirationWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly BackgroundJobOptions _options;
    private readonly ILogger<ViewingSessionExpirationWorker> _logger;

    public ViewingSessionExpirationWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<BackgroundJobOptions> options,
        ILogger<ViewingSessionExpirationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(1, _options.ViewingSessionExpirationIntervalSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ExpireSessionsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while expiring viewing sessions.");
            }

            await Task.Delay(interval, stoppingToken).ContinueWith(_ => { }, TaskScheduler.Default);
        }
    }

    private async Task ExpireSessionsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var liveStreamService = scope.ServiceProvider.GetRequiredService<ILiveStreamService>();
        var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();

        var now = clock.UtcNow;

        // Only the tokens that actually need revoking are fetched, and the pass is capped so a
        // backlog is drained over several ticks instead of in one oversized query and write.
        var expiredSessions = await dbContext.ViewingSessions
            .Where(v => v.Status == ViewingSessionStatus.ACTIVE && v.ExpiresAtUtc <= now)
            .OrderBy(v => v.ExpiresAtUtc)
            .Take(Math.Max(1, _options.ViewingSessionExpirationBatchSize))
            .Include(v => v.StreamTokens.Where(t => t.Status == StreamTokenStatus.ACTIVE))
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        if (expiredSessions.Count == 0)
        {
            return;
        }

        foreach (var session in expiredSessions)
        {
            session.Status = ViewingSessionStatus.EXPIRED;
            session.EndReason = ViewingEndReason.SESSION_EXPIRED;
            session.EndedAtUtc = now;

            foreach (var token in session.StreamTokens)
            {
                token.Status = StreamTokenStatus.EXPIRED;
                token.RevokedAtUtc = now;
            }

            try
            {
                await liveStreamService.StopAsync(new StopStreamRequest(session.Id, MediaSessionReference: null), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to notify media gateway of expired session {SessionId}.", session.Id);
            }

            await auditService.LogAsync(new AuditEvent(
                UserId: null,
                Action: "VIEWING_SESSION_EXPIRED",
                EntityType: nameof(ViewingSession),
                EntityId: session.Id.ToString(),
                Result: "SUCCESS"), cancellationToken);
        }

        // Session state, token revocation, and every staged audit row commit together.
        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Expired {Count} viewing session(s).", expiredSessions.Count);
    }
}
