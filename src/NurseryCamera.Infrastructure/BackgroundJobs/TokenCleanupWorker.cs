using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NurseryCamera.Application.Abstractions.Time;
using NurseryCamera.Application.Common.Options;
using NurseryCamera.Domain.Enums;
using NurseryCamera.Infrastructure.Persistence;

namespace NurseryCamera.Infrastructure.BackgroundJobs;

/// <summary>
/// Spec section 20: marks lapsed ACTIVE stream tokens as EXPIRED and periodically
/// deletes old, no-longer-active token records according to the retention policy.
/// </summary>
public sealed class TokenCleanupWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly BackgroundJobOptions _options;
    private readonly ILogger<TokenCleanupWorker> _logger;

    public TokenCleanupWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<BackgroundJobOptions> options,
        ILogger<TokenCleanupWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(Math.Max(1, _options.TokenCleanupIntervalMinutes));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while cleaning up stream tokens.");
            }

            await Task.Delay(interval, stoppingToken).ContinueWith(_ => { }, TaskScheduler.Default);
        }
    }

    private async Task CleanupAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();

        var now = clock.UtcNow;

        var lapsedCount = await dbContext.StreamTokens
            .Where(t => t.Status == StreamTokenStatus.ACTIVE && t.ExpiresAtUtc <= now)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(t => t.Status, StreamTokenStatus.EXPIRED)
                    .SetProperty(t => t.RevokedAtUtc, now),
                cancellationToken);

        var retentionCutoff = now.AddDays(-Math.Max(1, _options.TokenRetentionDays));

        var deletedCount = await dbContext.StreamTokens
            .Where(t => t.Status != StreamTokenStatus.ACTIVE && t.ExpiresAtUtc < retentionCutoff)
            .ExecuteDeleteAsync(cancellationToken);

        // CameraHealthWorker writes one row per camera per poll, so this table grows fastest of
        // all and would otherwise slow down every health/status read.
        var healthCutoff = now.AddDays(-Math.Max(1, _options.CameraHealthCheckRetentionDays));
        var deletedHealthChecks = await dbContext.CameraHealthChecks
            .Where(h => h.CheckedAtUtc < healthCutoff)
            .ExecuteDeleteAsync(cancellationToken);

        if (lapsedCount > 0 || deletedCount > 0 || deletedHealthChecks > 0)
        {
            _logger.LogInformation(
                "Cleanup: expired {LapsedCount} token(s), deleted {DeletedCount} retention-expired token(s) and {DeletedHealthChecks} health check(s).",
                lapsedCount,
                deletedCount,
                deletedHealthChecks);
        }
    }
}
