namespace NurseryCamera.Application.Features.ViewingSessions.Dtos;

public sealed record ViewingSessionDto(
    Guid Id,
    Guid ChildId,
    Guid CameraId,
    string Status,
    DateTime StartedAtUtc,
    DateTime ExpiresAtUtc,
    DateTime? EndedAtUtc,
    string? EndReason);
