using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NurseryCamera.Application.Abstractions.Time;
using NurseryCamera.Application.Common.Options;
using NurseryCamera.Infrastructure.Persistence;

namespace NurseryCamera.Infrastructure.BackgroundJobs;

/// <summary>
/// Spec section 33: processes reliable integration events written to the outbox
/// (ChildCheckedIn, ViewingSessionStarted, etc.) and marks them ProcessedAtUtc.
/// Real-time delivery (e.g. via SignalR hub) can be wired in here later without
/// changing the outbox contract.
/// </summary>
public sealed class OutboxWorker : BackgroundService
{
    private const int BatchSize = 50;
    private const int MaxRetryCount = 5;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly BackgroundJobOptions _options;
    private readonly ILogger<OutboxWorker> _logger;

    public OutboxWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<BackgroundJobOptions> options,
        ILogger<OutboxWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(1, _options.OutboxPollIntervalSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while processing outbox messages.");
            }

            await Task.Delay(interval, stoppingToken).ContinueWith(_ => { }, TaskScheduler.Default);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();

        var messages = await dbContext.OutboxMessages
            .Where(m => m.ProcessedAtUtc == null && m.RetryCount < MaxRetryCount)
            .OrderBy(m => m.OccurredAtUtc)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0)
        {
            return;
        }

        foreach (var message in messages)
        {
            try
            {
                // Delivery is a no-op stub today (SignalR hub can subscribe here later);
                // the important guarantee is that the message is durably marked processed.
                _logger.LogDebug("Processing outbox message {MessageId} of type {Type}.", message.Id, message.Type);
                message.ProcessedAtUtc = clock.UtcNow;
                message.Error = null;
            }
            catch (Exception ex)
            {
                message.RetryCount++;
                message.Error = ex.Message;
                _logger.LogWarning(ex, "Failed to process outbox message {MessageId}.", message.Id);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
