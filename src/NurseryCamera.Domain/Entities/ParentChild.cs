namespace NurseryCamera.Domain.Entities;

public class ParentChild
{
    public Guid ParentId { get; set; }
    public Guid ChildId { get; set; }
    public string RelationshipType { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public bool CanViewCamera { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public Parent Parent { get; set; } = null!;
    public Child Child { get; set; } = null!;
}
