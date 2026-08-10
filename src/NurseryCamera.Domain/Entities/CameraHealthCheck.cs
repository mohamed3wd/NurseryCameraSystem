using NurseryCamera.Domain.Enums;

namespace NurseryCamera.Domain.Entities;

public class CameraHealthCheck
{
    public Guid Id { get; set; }
    public Guid CameraId { get; set; }
    public DateTime CheckedAtUtc { get; set; }
    public HealthCheckStatus Status { get; set; }
    public int? LatencyMs { get; set; }
    public string? ErrorCode { get; set; }

    public Camera Camera { get; set; } = null!;
}
