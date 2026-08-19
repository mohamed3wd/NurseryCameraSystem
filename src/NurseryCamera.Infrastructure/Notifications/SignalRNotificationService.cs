using System.Text.Json;
using Microsoft.Extensions.Logging;
using NurseryCamera.Application.Abstractions.Notifications;
using NurseryCamera.Application.Abstractions.Time;
using NurseryCamera.Domain.Entities;
using NurseryCamera.Infrastructure.Persistence;

namespace NurseryCamera.Infrastructure.Notifications;

/// <summary>
/// Reliability-first notification stub: writes each notification to the transactional
/// outbox (spec section 33) rather than pushing directly, so a SignalR hub push added
/// later (see NurseryCamera.Api Hubs, "/hubs/nursery") can never lose an event on a
/// transient failure. The OutboxWorker is responsible for eventual delivery.
/// Per spec section 22, this is a courtesy signal only - server-side authorization and
/// session revocation always happen independently of whether this notification is delivered.
///
/// Messages are staged on the change tracker and flushed by <c>UnitOfWorkBehavior</c> together
/// with the state change that produced them, which both removes a round trip per notification
/// and makes the enqueue genuinely transactional with that state change.
/// </summary>
public sealed class SignalRNotificationService : INotificationService
{
    private readonly AppDbContext _dbContext;
    private readonly IClock _clock;
    private readonly ILogger<SignalRNotificationService> _logger;

    public SignalRNotificationService(AppDbContext dbContext, IClock clock, ILogger<SignalRNotificationService> logger)
    {
        _dbContext = dbContext;
        _clock = clock;
        _logger = logger;
    }

    public Task NotifyChildCheckedInAsync(Guid childId, IReadOnlyCollection<Guid> parentUserIds, DateTime checkInUtc, CancellationToken cancellationToken = default)
        => EnqueueAsync("ChildCheckedIn", new { ChildId = childId, ParentUserIds = parentUserIds, CheckInUtc = checkInUtc }, cancellationToken);

    public Task NotifyChildCheckedOutAsync(Guid childId, IReadOnlyCollection<Guid> parentUserIds, DateTime checkOutUtc, CancellationToken cancellationToken = default)
        => EnqueueAsync("ChildCheckedOut", new { ChildId = childId, ParentUserIds = parentUserIds, CheckOutUtc = checkOutUtc }, cancellationToken);

    public Task NotifyViewingSessionRevokedAsync(Guid viewingSessionId, Guid parentUserId, string reason, CancellationToken cancellationToken = default)
        => EnqueueAsync("ViewingSessionRevoked", new { ViewingSessionId = viewingSessionId, ParentUserId = parentUserId, Reason = reason }, cancellationToken);

    public Task NotifyCameraStatusChangedAsync(Guid cameraId, string status, CancellationToken cancellationToken = default)
        => EnqueueAsync("CameraStatusChanged", new { CameraId = cameraId, Status = status }, cancellationToken);

    private Task EnqueueAsync(string eventType, object payload, CancellationToken cancellationToken)
    {
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = eventType,
            PayloadJson = JsonSerializer.Serialize(payload),
            OccurredAtUtc = _clock.UtcNow
        };

        _dbContext.OutboxMessages.Add(outboxMessage);

        _logger.LogDebug(
            "Queued notification {EventType} via outbox message {OutboxMessageId}.",
            eventType,
            outboxMessage.Id);

        return Task.CompletedTask;
    }
}
