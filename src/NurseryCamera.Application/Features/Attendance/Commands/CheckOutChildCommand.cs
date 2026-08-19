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
using NurseryCamera.Application.Features.Attendance.Dtos;
using NurseryCamera.Domain.Entities;
using NurseryCamera.Domain.Enums;

namespace NurseryCamera.Application.Features.Attendance.Commands;

/// <summary>
/// Staff-only. See spec section 10 (Check-Out Flow) and BR-009/BR-010: checking a child out
/// must immediately terminate every active viewing session and revoke every active stream
/// token for that child.
/// </summary>
public sealed record CheckOutChildCommand(Guid ChildId) : IRequest<AttendanceDto>;

public sealed class CheckOutChildCommandValidator : AbstractValidator<CheckOutChildCommand>
{
    public CheckOutChildCommandValidator()
    {
        RuleFor(x => x.ChildId).NotEmpty();
    }
}

public sealed class CheckOutChildCommandHandler : IRequestHandler<CheckOutChildCommand, AttendanceDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly IAuditService _auditService;
    private readonly INotificationService _notificationService;
    private readonly ILiveStreamService _liveStreamService;
    private readonly ILogger<CheckOutChildCommandHandler> _logger;

    public CheckOutChildCommandHandler(
        IAppDbContext db,
        ICurrentUser currentUser,
        IClock clock,
        IAuditService auditService,
        INotificationService notificationService,
        ILiveStreamService liveStreamService,
        ILogger<CheckOutChildCommandHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _auditService = auditService;
        _notificationService = notificationService;
        _liveStreamService = liveStreamService;
        _logger = logger;
    }

    public async Task<AttendanceDto> Handle(CheckOutChildCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            throw AppException.AuthenticationRequired();
        }

        var staff = await _db.Staff
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId && s.IsActive, cancellationToken);

        if (staff is null)
        {
            throw AppException.Forbidden(message: "Only active staff can check out a child.");
        }

        var attendance = await _db.AttendanceSessions
            .FirstOrDefaultAsync(a => a.ChildId == request.ChildId && a.Status == AttendanceStatus.PRESENT, cancellationToken);

        if (attendance is null)
        {
            throw new NurseryCamera.Domain.Exceptions.ChildNotPresentException();
        }

        var now = _clock.UtcNow;
        attendance.CheckOutUtc = now;
        attendance.Status = AttendanceStatus.COMPLETED;

        var activeSessions = await _db.ViewingSessions
            .Where(v => v.ChildId == request.ChildId
                        && (v.Status == ViewingSessionStatus.PENDING || v.Status == ViewingSessionStatus.ACTIVE))
            .ToListAsync(cancellationToken);

        var sessionIds = activeSessions.Select(s => s.Id).ToList();

        var activeTokens = sessionIds.Count == 0
            ? new List<StreamToken>()
            : await _db.StreamTokens
                .Where(t => sessionIds.Contains(t.ViewingSessionId) && t.Status == StreamTokenStatus.ACTIVE)
                .ToListAsync(cancellationToken);

        foreach (var session in activeSessions)
        {
            session.Status = ViewingSessionStatus.ENDED;
            session.EndedAtUtc = now;
            session.EndReason = ViewingEndReason.CHILD_CHECKED_OUT;
        }

        foreach (var token in activeTokens)
        {
            token.Status = StreamTokenStatus.REVOKED;
            token.RevokedAtUtc = now;
        }

        // A single SaveChangesAsync call persists attendance completion, viewing-session
        // termination, token revocation, and the audit/outbox trail below as one atomic
        // database transaction (BR-009/BR-010); UnitOfWorkBehavior issues it.
        await _auditService.LogAsync(
            new AuditEvent(userId, "CHILD_CHECK_OUT", "AttendanceSession", attendance.Id.ToString(), "SUCCESS",
                Metadata: new { attendance.ChildId, RevokedSessions = sessionIds.Count }),
            cancellationToken);

        foreach (var sessionId in sessionIds)
        {
            await _auditService.LogAsync(
                new AuditEvent(userId, "VIEWING_SESSION_ENDED", "ViewingSession", sessionId.ToString(), "SUCCESS",
                    Metadata: new { Reason = ViewingEndReason.CHILD_CHECKED_OUT.ToString() }),
                cancellationToken);
        }

        // External media gateway termination happens outside the DB transaction (spec section 32);
        // best-effort here, failures are logged rather than rolling back the check-out.
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
        }

        var parentUserIds = await _db.ParentChildren
            .AsNoTracking()
            .Where(pc => pc.ChildId == request.ChildId)
            .Select(pc => pc.Parent.UserId)
            .ToListAsync(cancellationToken);

        await _notificationService.NotifyChildCheckedOutAsync(request.ChildId, parentUserIds, now, cancellationToken);

        // One lookup for every revoked session's parent instead of a query per session: a child
        // checked out during a busy viewing window can easily have several concurrent sessions.
        var revokedParentIds = activeSessions.Select(s => s.ParentId).Distinct().ToList();
        var userIdByParentId = revokedParentIds.Count == 0
            ? new Dictionary<Guid, Guid>()
            : await _db.Parents
                .AsNoTracking()
                .Where(p => revokedParentIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.UserId, cancellationToken);

        foreach (var session in activeSessions)
        {
            if (userIdByParentId.TryGetValue(session.ParentId, out var parentUserId) && parentUserId != Guid.Empty)
            {
                await _notificationService.NotifyViewingSessionRevokedAsync(session.Id, parentUserId, "CHILD_CHECKED_OUT", cancellationToken);
            }
        }

        return new AttendanceDto(
            attendance.Id, attendance.ChildId, attendance.StaffId, attendance.CheckInUtc, attendance.CheckOutUtc,
            attendance.Status.ToString(), attendance.Source.ToString(), attendance.Notes);
    }
}
