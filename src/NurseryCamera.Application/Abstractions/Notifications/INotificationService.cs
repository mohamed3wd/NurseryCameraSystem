namespace NurseryCamera.Application.Abstractions.Notifications;

/// <summary>
/// Pushes real-time/user notifications (e.g. via SignalR, spec section 22). The server-side
/// authorization/session revocation always happens first; these notifications are a courtesy
/// signal to the client and must never be relied upon as the actual security boundary.
/// </summary>
public interface INotificationService
{
    Task NotifyChildCheckedInAsync(Guid childId, IReadOnlyCollection<Guid> parentUserIds, DateTime checkInUtc, CancellationToken cancellationToken = default);

    Task NotifyChildCheckedOutAsync(Guid childId, IReadOnlyCollection<Guid> parentUserIds, DateTime checkOutUtc, CancellationToken cancellationToken = default);

    Task NotifyViewingSessionRevokedAsync(Guid viewingSessionId, Guid parentUserId, string reason, CancellationToken cancellationToken = default);

    Task NotifyCameraStatusChangedAsync(Guid cameraId, string status, CancellationToken cancellationToken = default);
}
