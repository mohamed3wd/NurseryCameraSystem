namespace NurseryCamera.Domain.Events;

public sealed record CameraDisabled(
    Guid CameraId,
    Guid NurseryId,
    DateTime DisabledAtUtc);
