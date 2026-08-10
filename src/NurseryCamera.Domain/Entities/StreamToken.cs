using NurseryCamera.Domain.Enums;

namespace NurseryCamera.Domain.Entities;

public class StreamToken
{
    public Guid Id { get; set; }
    public Guid ViewingSessionId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime IssuedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public StreamTokenStatus Status { get; set; }

    public ViewingSession ViewingSession { get; set; } = null!;
}
