namespace NurseryCamera.Domain.Entities;

public class CameraRoom
{
    public Guid CameraId { get; set; }
    public Guid RoomId { get; set; }
    public DateTime ValidFromUtc { get; set; }
    public DateTime? ValidToUtc { get; set; }

    public Camera Camera { get; set; } = null!;
    public Room Room { get; set; } = null!;
}
