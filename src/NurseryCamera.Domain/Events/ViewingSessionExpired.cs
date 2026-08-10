namespace NurseryCamera.Domain.Events;

public sealed record ViewingSessionExpired(
    Guid ViewingSessionId,
    Guid ParentId,
    Guid ChildId,
    Guid CameraId,
    DateTime ExpiredAtUtc);
