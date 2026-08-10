namespace NurseryCamera.Domain.Events;

public sealed record ViewingSessionStarted(
    Guid ViewingSessionId,
    Guid ParentId,
    Guid ChildId,
    Guid CameraId,
    Guid AttendanceSessionId,
    DateTime StartedAtUtc,
    DateTime ExpiresAtUtc);
