using NurseryCamera.Domain.Enums;

namespace NurseryCamera.Domain.Entities;

public class Camera
{
    public Guid Id { get; set; }
    public Guid NurseryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string RtspUrlEncrypted { get; set; } = string.Empty;
    public string UsernameEncrypted { get; set; } = string.Empty;
    public string PasswordEncrypted { get; set; } = string.Empty;
    public CameraStatus Status { get; set; }
    public string? StreamProfile { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastHealthCheckUtc { get; set; }

    public Nursery Nursery { get; set; } = null!;
    public ICollection<CameraRoom> CameraRooms { get; set; } = new List<CameraRoom>();
    public ICollection<ViewingSession> ViewingSessions { get; set; } = new List<ViewingSession>();
    public ICollection<CameraHealthCheck> HealthChecks { get; set; } = new List<CameraHealthCheck>();
}
