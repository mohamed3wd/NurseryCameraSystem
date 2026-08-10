namespace NurseryCamera.Domain.Events;

public sealed record UnauthorizedCameraAccessAttempted(
    Guid? UserId,
    Guid? ParentId,
    Guid ChildId,
    Guid CameraId,
    string Reason,
    DateTime AttemptedAtUtc);
