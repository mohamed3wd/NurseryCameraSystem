namespace NurseryCamera.Domain.Entities;

public class Room
{
    public Guid Id { get; set; }
    public Guid NurseryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? RoomType { get; set; }
    public bool IsActive { get; set; }

    public Nursery Nursery { get; set; } = null!;
    public ICollection<Child> Children { get; set; } = new List<Child>();
    public ICollection<CameraRoom> CameraRooms { get; set; } = new List<CameraRoom>();
}
