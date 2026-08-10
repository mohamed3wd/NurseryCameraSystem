namespace NurseryCamera.Application.Features.Parents.Dtos;

public sealed record ChildDto(
    Guid Id,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    Guid? RoomId,
    string? RoomName,
    string EnrollmentStatus,
    bool IsActive,
    bool CanViewCamera);
