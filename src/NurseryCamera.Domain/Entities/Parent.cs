using NurseryCamera.Domain.Enums;

namespace NurseryCamera.Domain.Entities;

public class Parent
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public ParentStatus Status { get; set; }

    public ICollection<ParentChild> ParentChildren { get; set; } = new List<ParentChild>();
    public ICollection<ViewingSession> ViewingSessions { get; set; } = new List<ViewingSession>();
}
