namespace NurseryCamera.Application.Features.Administration.Dtos;

public sealed record NurseryDto(
    Guid Id,
    string Name,
    string TimeZoneId,
    string? Address,
    bool IsActive);
