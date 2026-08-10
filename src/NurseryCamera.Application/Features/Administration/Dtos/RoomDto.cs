namespace NurseryCamera.Application.Features.Administration.Dtos;

public sealed record RoomDto(
    Guid Id,
    Guid NurseryId,
    string Name,
    string Code,
    string? RoomType,
    bool IsActive);
