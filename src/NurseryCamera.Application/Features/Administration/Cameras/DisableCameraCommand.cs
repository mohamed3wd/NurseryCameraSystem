using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NurseryCamera.Application.Abstractions.Audit;
using NurseryCamera.Application.Abstractions.Identity;
using NurseryCamera.Application.Abstractions.Notifications;
using NurseryCamera.Application.Abstractions.Persistence;
using NurseryCamera.Application.Abstractions.Streaming;
using NurseryCamera.Application.Abstractions.Time;
using NurseryCamera.Application.Common.Exceptions;
using NurseryCamera.Domain.Enums;

namespace NurseryCamera.Application.Features.Administration.Cameras;

/// <summary>
/// Disabling a camera must immediately end every active/pending viewing session against it,
/// consistent with the fail-closed principle (BR-022) - a disabled camera can never keep
/// streaming to a parent who already started viewing it.
/// </summary>
public sealed record DisableCameraCommand(Guid CameraId) : IRequest;

public sealed class DisableCameraCommandValidator : AbstractValidator<DisableCameraCommand>
{
    public DisableCameraCommandValidator() => RuleFor(x => x.CameraId).NotEmpty();
}

public sealed class DisableCameraCommandHandler : IRequestHandler<DisableCameraCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly IAuditService _auditService;
    private readonly INotificationService _notificationService;
    private readonly ILiveStreamService _liveStreamService;
    private readonly ILogger<DisableCameraCommandHandler> _logger;

    public DisableCameraCommandHandler(
        IAppDbContext db,
        ICurrentUser currentUser,
        IClock clock,
        IAuditService auditService,
        INotificationService notificationService,
        ILiveStreamService liveStreamService,
        ILogger<DisableCameraCommandHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _auditService = auditService;
        _notificationService = notificationService;
        _liveStreamService = liveStreamService;
        _logger = logger;
    }

    public async Task Handle(DisableCameraCommand request, CancellationToken cancellationToken)
    {
        var camera = await _db.Cameras.FirstOrDefaultAsync(c => c.Id == request.CameraId, cancellationToken)
                     ?? throw AppException.NotFound("CAMERA_NOT_FOUND", "Camera not found.");

        camera.IsActive = false;
        camera.Status = CameraStatus.INACTIVE;

        var now = _clock.UtcNow;
        var activeSessions = await _db.ViewingSessions
            .Where(v => v.CameraId == request.CameraId
                        && (v.Status == ViewingSessionStatus.PENDING || v.Status == ViewingSessionStatus.ACTIVE))
            .ToListAsync(cancellationToken);

        var sessionIds = activeSessions.Select(s => s.Id).ToList();
        var tokens = sessionIds.Count == 0
            ? new List<Domain.Entities.StreamToken>()
            : await _db.StreamTokens
                .Where(t => sessionIds.Contains(t.ViewingSessionId) && t.Status == StreamTokenStatus.ACTIVE)
                .ToListAsync(cancellationToken);

        foreach (var session in activeSessions)
        {
            session.Status = ViewingSessionStatus.REVOKED;
            session.EndedAtUtc = now;
            session.EndReason = ViewingEndReason.ADMIN_REVOKED;
        }

        foreach (var token in tokens)
        {
            token.Status = StreamTokenStatus.REVOKED;
            token.RevokedAtUtc = now;
        }

        await _auditService.LogAsync(
            new AuditEvent(_currentUser.UserId, "CAMERA_DISABLED", "Camera", camera.Id.ToString(), "SUCCESS",
                Metadata: new { RevokedSessions = sessionIds.Count }),
            cancellationToken);

        var revokedParentIds = activeSessions.Select(s => s.ParentId).Distinct().ToList();
        var userIdByParentId = revokedParentIds.Count == 0
            ? new Dictionary<Guid, Guid>()
            : await _db.Parents
                .AsNoTracking()
                .Where(p => revokedParentIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.UserId, cancellationToken);

        foreach (var session in activeSessions)
        {
            try
            {
                await _liveStreamService.StopAsync(new StopStreamRequest(session.Id, null), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to stop media gateway session for viewing session {ViewingSessionId}", session.Id);
            }

            if (userIdByParentId.TryGetValue(session.ParentId, out var parentUserId) && parentUserId != Guid.Empty)
            {
                await _notificationService.NotifyViewingSessionRevokedAsync(session.Id, parentUserId, "ADMIN_REVOKED", cancellationToken);
            }
        }

        await _notificationService.NotifyCameraStatusChangedAsync(camera.Id, camera.Status.ToString(), cancellationToken);
    }
}
