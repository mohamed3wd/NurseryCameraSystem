namespace NurseryCamera.Domain.Entities;

public class Nursery
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TimeZoneId { get; set; } = string.Empty;
    public string? Address { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public ICollection<Room> Rooms { get; set; } = new List<Room>();
    public ICollection<Child> Children { get; set; } = new List<Child>();
    public ICollection<Camera> Cameras { get; set; } = new List<Camera>();
    public ICollection<Staff> Staff { get; set; } = new List<Staff>();
}
