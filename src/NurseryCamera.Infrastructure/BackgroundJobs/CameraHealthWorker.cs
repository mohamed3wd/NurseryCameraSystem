using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NurseryCamera.Application.Abstractions.Notifications;
using NurseryCamera.Application.Abstractions.Time;
using NurseryCamera.Application.Common.Options;
using NurseryCamera.Domain.Entities;
using NurseryCamera.Domain.Enums;
using NurseryCamera.Infrastructure.Persistence;

namespace NurseryCamera.Infrastructure.BackgroundJobs;

/// <summary>
/// Spec section 20: periodically checks each active camera's health, records a
/// CameraHealthCheck, updates the camera status, and notifies on state changes.
/// The MVP mock implementation reports every active camera as healthy.
/// </summary>
public sealed class CameraHealthWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly BackgroundJobOptions _options;
    private readonly ILogger<CameraHealthWorker> _logger;

    public CameraHealthWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<BackgroundJobOptions> options,
        ILogger<CameraHealthWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(5, _options.CameraHealthCheckIntervalSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckCamerasAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while running camera health checks.");
            }

            await Task.Delay(interval, stoppingToken).ContinueWith(_ => { }, TaskScheduler.Default);
        }
    }

    private async Task CheckCamerasAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();

        var now = clock.UtcNow;

        // Projected rather than materialized as entities: this runs on a timer forever and the
        // Camera row carries the encrypted RTSP URL and credentials, which the probe never needs.
        var cameras = await dbContext.Cameras
            .AsNoTracking()
            .Where(c => c.IsActive && c.Status != CameraStatus.MAINTENANCE && c.Status != CameraStatus.INACTIVE)
            .Select(c => new { c.Id, c.Status })
            .ToListAsync(cancellationToken);

        if (cameras.Count == 0)
        {
            return;
        }

        foreach (var camera in cameras)
        {
            // MVP mock: no real RTSP probe. A future gateway implementation would ping
            // the camera/media gateway here instead of assuming success.
            dbContext.CameraHealthChecks.Add(new CameraHealthCheck
            {
                Id = Guid.NewGuid(),
                CameraId = camera.Id,
                CheckedAtUtc = now,
                Status = HealthCheckStatus.Healthy,
                LatencyMs = 0
            });

            if (camera.Status != CameraStatus.ACTIVE)
            {
                await notificationService.NotifyCameraStatusChangedAsync(
                    camera.Id,
                    CameraStatus.ACTIVE.ToString(),
                    cancellationToken);
            }
        }

        await dbContext.Cameras
            .Where(c => c.IsActive && c.Status != CameraStatus.MAINTENANCE && c.Status != CameraStatus.INACTIVE)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(c => c.Status, CameraStatus.ACTIVE)
                    .SetProperty(c => c.LastHealthCheckUtc, now),
                cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
