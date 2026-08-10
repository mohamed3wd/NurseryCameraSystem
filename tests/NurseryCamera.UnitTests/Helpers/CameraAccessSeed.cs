using NurseryCamera.Domain.Entities;
using NurseryCamera.Domain.Enums;
using NurseryCamera.Infrastructure.Persistence;

namespace NurseryCamera.UnitTests.Helpers;

/// <summary>
/// Seeds a fully authorized parent → child → attendance → room → camera graph
/// for CameraAccessPolicy / check-out tests (spec §8 / §37 / §38).
/// </summary>
internal sealed class CameraAccessSeed
{
    public Guid UserId { get; } = Guid.NewGuid();
    public Guid ParentId { get; } = Guid.NewGuid();
    public Guid ChildId { get; } = Guid.NewGuid();
    public Guid CameraId { get; } = Guid.NewGuid();
    public Guid NurseryId { get; } = Guid.NewGuid();
    public Guid RoomId { get; } = Guid.NewGuid();
    public Guid AttendanceSessionId { get; } = Guid.NewGuid();
    public Guid StaffId { get; } = Guid.NewGuid();
    public Guid StaffUserId { get; } = Guid.NewGuid();

    public Parent Parent { get; private set; } = null!;
    public ParentChild Relation { get; private set; } = null!;
    public Child Child { get; private set; } = null!;
    public Camera Camera { get; private set; } = null!;
    public CameraRoom CameraRoom { get; private set; } = null!;
    public AttendanceSession Attendance { get; private set; } = null!;
    public Staff Staff { get; private set; } = null!;

    public static async Task<CameraAccessSeed> CreateAuthorizedAsync(
        AppDbContext db,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        var seed = new CameraAccessSeed();

        var nursery = new Nursery
        {
            Id = seed.NurseryId,
            Name = "Test Nursery",
            TimeZoneId = "UTC",
            IsActive = true,
            CreatedAtUtc = utcNow
        };

        var room = new Room
        {
            Id = seed.RoomId,
            NurseryId = seed.NurseryId,
            Name = "Room A",
            Code = "A1",
            IsActive = true
        };

        seed.Parent = new Parent
        {
            Id = seed.ParentId,
            UserId = seed.UserId,
            Status = ParentStatus.Active
        };

        seed.Child = new Child
        {
            Id = seed.ChildId,
            NurseryId = seed.NurseryId,
            RoomId = seed.RoomId,
            FirstName = "Ada",
            LastName = "Lovelace",
            DateOfBirth = new DateOnly(2020, 1, 1),
            EnrollmentStatus = EnrollmentStatus.Active,
            IsActive = true
        };

        seed.Relation = new ParentChild
        {
            ParentId = seed.ParentId,
            ChildId = seed.ChildId,
            RelationshipType = "Mother",
            IsPrimary = true,
            CanViewCamera = true,
            CreatedAtUtc = utcNow
        };

        seed.Camera = new Camera
        {
            Id = seed.CameraId,
            NurseryId = seed.NurseryId,
            Name = "Cam 1",
            RtspUrlEncrypted = "enc-rtsp",
            UsernameEncrypted = "enc-user",
            PasswordEncrypted = "enc-pass",
            Status = CameraStatus.ACTIVE,
            IsActive = true
        };

        seed.CameraRoom = new CameraRoom
        {
            CameraId = seed.CameraId,
            RoomId = seed.RoomId,
            ValidFromUtc = utcNow.AddDays(-1),
            ValidToUtc = null
        };

        seed.Staff = new Staff
        {
            Id = seed.StaffId,
            UserId = seed.StaffUserId,
            NurseryId = seed.NurseryId,
            EmployeeNumber = "S-001",
            Role = StaffRole.Teacher,
            IsActive = true
        };

        seed.Attendance = new AttendanceSession
        {
            Id = seed.AttendanceSessionId,
            ChildId = seed.ChildId,
            StaffId = seed.StaffId,
            CheckInUtc = utcNow.AddHours(-2),
            Status = AttendanceStatus.PRESENT,
            Source = AttendanceSource.Manual
        };

        db.Nurseries.Add(nursery);
        db.Rooms.Add(room);
        db.Parents.Add(seed.Parent);
        db.Children.Add(seed.Child);
        db.ParentChildren.Add(seed.Relation);
        db.Cameras.Add(seed.Camera);
        db.CameraRooms.Add(seed.CameraRoom);
        db.Staff.Add(seed.Staff);
        db.AttendanceSessions.Add(seed.Attendance);
        await db.SaveChangesAsync(cancellationToken);

        return seed;
    }
}
