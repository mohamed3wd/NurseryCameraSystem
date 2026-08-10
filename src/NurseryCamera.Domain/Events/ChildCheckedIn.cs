namespace NurseryCamera.Domain.Events;

public sealed record ChildCheckedIn(
    Guid ChildId,
    Guid AttendanceSessionId,
    Guid? StaffId,
    DateTime CheckInUtc);
