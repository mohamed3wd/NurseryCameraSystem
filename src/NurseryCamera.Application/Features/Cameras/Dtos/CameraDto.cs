namespace NurseryCamera.Application.Features.Cameras.Dtos;

/// <summary>Never includes RTSP URL, username, password, or any camera infrastructure detail (BR-014).</summary>
public sealed record CameraDto(
    Guid Id,
    string Name,
    string? Location,
    string Status,
    bool IsAvailable);
