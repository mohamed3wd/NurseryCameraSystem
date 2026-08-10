namespace NurseryCamera.Domain.Events;

public sealed record CameraWentOffline(
    Guid CameraId,
    Guid NurseryId,
    DateTime OfflineAtUtc);
