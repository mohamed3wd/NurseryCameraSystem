namespace NurseryCamera.Application.Features.Attendance.Dtos;

public sealed record AttendanceDto(
    Guid Id,
    Guid ChildId,
    Guid? StaffId,
    DateTime CheckInUtc,
    DateTime? CheckOutUtc,
    string Status,
    string Source,
    string? Notes);
