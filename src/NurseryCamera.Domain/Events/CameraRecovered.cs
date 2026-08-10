namespace NurseryCamera.Domain.Events;

public sealed record CameraRecovered(
    Guid CameraId,
    Guid NurseryId,
    DateTime RecoveredAtUtc);
