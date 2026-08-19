using System.Text.Json;
using NurseryCamera.Application.Abstractions.Audit;
using NurseryCamera.Application.Abstractions.Time;
using NurseryCamera.Domain.Entities;
using NurseryCamera.Domain.Enums;
using NurseryCamera.Infrastructure.Persistence;

namespace NurseryCamera.Infrastructure.Audit;

/// <summary>
/// Stages audit log records (spec section 25) and, for security-sensitive action types,
/// also a SecurityEvent so denial/anomaly monitoring doesn't require scanning the full audit
/// trail. Never persists raw passwords, camera credentials, or raw stream tokens.
///
/// Rows are only added to the change tracker here. <c>UnitOfWorkBehavior</c> flushes them with
/// the rest of the request, so a handler that writes several audit entries still costs a single
/// database round trip. Callers outside the MediatR pipeline (background workers) must save
/// their own scope's <c>DbContext</c> after logging.
/// </summary>
public sealed class AuditService : IAuditService
{
    /// <summary>Audit actions from spec section 25 that also warrant a SecurityEvent record.</summary>
    private static readonly IReadOnlyDictionary<string, SecurityEventSeverity> SecurityRelevantActions =
        new Dictionary<string, SecurityEventSeverity>(StringComparer.OrdinalIgnoreCase)
        {
            ["LOGIN_FAILED"] = SecurityEventSeverity.Medium,
            ["CAMERA_VIEW_DENIED"] = SecurityEventSeverity.Medium,
            ["SECURITY_POLICY_DENIED"] = SecurityEventSeverity.High,
            ["RATE_LIMIT_EXCEEDED"] = SecurityEventSeverity.Medium,
            ["VIEWING_SESSION_REVOKED"] = SecurityEventSeverity.Medium
        };

    private readonly AppDbContext _dbContext;
    private readonly IClock _clock;

    public AuditService(AppDbContext dbContext, IClock clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    public Task LogAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var metadataJson = auditEvent.Metadata is null ? null : JsonSerializer.Serialize(auditEvent.Metadata);

        var auditLog = new AuditLog
        {
            UserId = auditEvent.UserId,
            Action = auditEvent.Action,
            EntityType = auditEvent.EntityType,
            EntityId = auditEvent.EntityId,
            Result = auditEvent.Result,
            IpHash = auditEvent.IpHash,
            MetadataJson = metadataJson,
            CreatedAtUtc = now
        };
        _dbContext.AuditLogs.Add(auditLog);

        if (SecurityRelevantActions.TryGetValue(auditEvent.Action, out var severity) ||
            string.Equals(auditEvent.Result, "DENIED", StringComparison.OrdinalIgnoreCase))
        {
            _dbContext.SecurityEvents.Add(new SecurityEvent
            {
                UserId = auditEvent.UserId,
                EventType = auditEvent.Action,
                Severity = severity == default ? SecurityEventSeverity.Medium : severity,
                IpHash = auditEvent.IpHash,
                MetadataJson = metadataJson,
                CreatedAtUtc = now
            });
        }

        return Task.CompletedTask;
    }
}
