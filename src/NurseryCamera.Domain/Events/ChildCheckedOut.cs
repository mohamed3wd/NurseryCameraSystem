namespace NurseryCamera.Domain.Events;

public sealed record ChildCheckedOut(
    Guid ChildId,
    Guid AttendanceSessionId,
    DateTime CheckOutUtc);
