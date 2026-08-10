using NurseryCamera.Domain.Enums;

namespace NurseryCamera.Domain.Entities;

public class AttendanceSession
{
    public Guid Id { get; set; }
    public Guid ChildId { get; set; }
    public Guid? StaffId { get; set; }
    public DateTime CheckInUtc { get; set; }
    public DateTime? CheckOutUtc { get; set; }
    public AttendanceStatus Status { get; set; }
    public AttendanceSource Source { get; set; }
    public string? Notes { get; set; }

    public Child Child { get; set; } = null!;
    public Staff? Staff { get; set; }
    public ICollection<ViewingSession> ViewingSessions { get; set; } = new List<ViewingSession>();
}
