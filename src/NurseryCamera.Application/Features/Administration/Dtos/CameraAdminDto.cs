namespace NurseryCamera.Application.Features.Administration.Dtos;

/// <summary>Admin view of a camera. Never includes RTSP URL/username/password (BR-014).</summary>
public sealed record CameraAdminDto(
    Guid Id,
    Guid NurseryId,
    string Name,
    string? Location,
    string Status,
    string? StreamProfile,
    bool IsActive,
    DateTime? LastHealthCheckUtc,
    IReadOnlyList<Guid> RoomIds);
