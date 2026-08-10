using NurseryCamera.Domain.Enums;

namespace NurseryCamera.Domain.Entities;

public class ViewingSession
{
    public Guid Id { get; set; }
    public Guid ParentId { get; set; }
    public Guid ChildId { get; set; }
    public Guid CameraId { get; set; }
    public Guid AttendanceSessionId { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? EndedAtUtc { get; set; }
    public ViewingSessionStatus Status { get; set; }
    public ViewingEndReason? EndReason { get; set; }
    public string? ClientType { get; set; }
    public string? DeviceIdHash { get; set; }

    public Parent Parent { get; set; } = null!;
    public Child Child { get; set; } = null!;
    public Camera Camera { get; set; } = null!;
    public AttendanceSession AttendanceSession { get; set; } = null!;
    public ICollection<StreamToken> StreamTokens { get; set; } = new List<StreamToken>();
}
