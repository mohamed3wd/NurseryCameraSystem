using NurseryCamera.Domain.Enums;

namespace NurseryCamera.Domain.Entities;

public class SecurityEvent
{
    public long Id { get; set; }
    public Guid? UserId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public SecurityEventSeverity Severity { get; set; }
    public string? IpHash { get; set; }
    public string? DeviceIdHash { get; set; }
    public string? MetadataJson { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
