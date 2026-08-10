using NurseryCamera.Domain.Enums;

namespace NurseryCamera.Domain.Entities;

public class Child
{
    public Guid Id { get; set; }
    public Guid NurseryId { get; set; }
    public Guid? RoomId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public EnrollmentStatus EnrollmentStatus { get; set; }
    public bool IsActive { get; set; }

    public Nursery Nursery { get; set; } = null!;
    public Room? Room { get; set; }
    public ICollection<ParentChild> ParentChildren { get; set; } = new List<ParentChild>();
    public ICollection<AttendanceSession> AttendanceSessions { get; set; } = new List<AttendanceSession>();
    public ICollection<ViewingSession> ViewingSessions { get; set; } = new List<ViewingSession>();
}
