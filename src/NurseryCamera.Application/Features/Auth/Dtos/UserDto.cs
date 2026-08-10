namespace NurseryCamera.Application.Features.Auth.Dtos;

public sealed record UserDto(
    Guid Id,
    string Email,
    string? FullName,
    string? Phone,
    bool IsActive,
    IReadOnlyList<string> Roles);
