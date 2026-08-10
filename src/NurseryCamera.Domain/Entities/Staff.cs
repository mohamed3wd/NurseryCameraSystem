using NurseryCamera.Domain.Enums;

namespace NurseryCamera.Domain.Entities;

public class Staff
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid NurseryId { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public StaffRole Role { get; set; }
    public bool IsActive { get; set; }

    public Nursery Nursery { get; set; } = null!;
    public ICollection<AttendanceSession> AttendanceSessions { get; set; } = new List<AttendanceSession>();
}
