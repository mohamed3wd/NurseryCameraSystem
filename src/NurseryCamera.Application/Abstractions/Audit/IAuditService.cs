namespace NurseryCamera.Application.Abstractions.Audit;

/// <summary>
/// Records security-sensitive actions (spec section 25). Every stream start/stop,
/// authorization denial, token issuance, check-in/out, and admin change must go through here.
/// Implementations must never persist raw passwords, camera credentials, or raw stream tokens.
/// </summary>
public interface IAuditService
{
    Task LogAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);
}

/// <summary>
/// A single audit entry. <see cref="Metadata"/> is serialized to JSON and must never contain secrets.
/// </summary>
public sealed record AuditEvent(
    Guid? UserId,
    string Action,
    string EntityType,
    string? EntityId,
    string Result,
    string? IpHash = null,
    object? Metadata = null);
