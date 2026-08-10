using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NurseryCamera.Application.Abstractions.Audit;
using NurseryCamera.Application.Abstractions.Identity;
using NurseryCamera.Application.Abstractions.Persistence;
using NurseryCamera.Application.Abstractions.Streaming;
using NurseryCamera.Application.Abstractions.Time;
using NurseryCamera.Application.Common.Exceptions;
using NurseryCamera.Domain.Enums;

namespace NurseryCamera.Application.Features.ViewingSessions.Commands;

public sealed record StopViewingSessionCommand(Guid SessionId) : IRequest;

public sealed class StopViewingSessionCommandValidator : AbstractValidator<StopViewingSessionCommand>
{
    public StopViewingSessionCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
    }
}

public sealed class StopViewingSessionCommandHandler : IRequestHandler<StopViewingSessionCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly ILiveStreamService _liveStreamService;
    private readonly IAuditService _auditService;
    private readonly ILogger<StopViewingSessionCommandHandler> _logger;

    public StopViewingSessionCommandHandler(
        IAppDbContext db,
        ICurrentUser currentUser,
        IClock clock,
        ILiveStreamService liveStreamService,
        IAuditService auditService,
        ILogger<StopViewingSessionCommandHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _liveStreamService = liveStreamService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task Handle(StopViewingSessionCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            throw AppException.AuthenticationRequired();
        }

        // Ownership must be verified via a join to Parents.UserId - never trust the session id
        // alone, otherwise any authenticated parent could stop another parent's session (IDOR).
        var session = await _db.ViewingSessions
            .FirstOrDefaultAsync(v => v.Id == request.SessionId && v.Parent.UserId == userId, cancellationToken);

        if (session is null)
        {
            throw AppException.NotFound("VIEWING_SESSION_NOT_FOUND", "Viewing session not found.");
        }

        if (session.Status is ViewingSessionStatus.PENDING or ViewingSessionStatus.ACTIVE)
        {
            var now = _clock.UtcNow;
            session.Status = ViewingSessionStatus.ENDED;
            session.EndedAtUtc = now;
            session.EndReason = ViewingEndReason.PARENT_STOPPED;

            var tokens = await _db.StreamTokens
                .Where(t => t.ViewingSessionId == session.Id && t.Status == StreamTokenStatus.ACTIVE)
                .ToListAsync(cancellationToken);

            foreach (var token in tokens)
            {
                token.Status = StreamTokenStatus.REVOKED;
                token.RevokedAtUtc = now;
            }

            await _db.SaveChangesAsync(cancellationToken);

            try
            {
                await _liveStreamService.StopAsync(new StopStreamRequest(session.Id, null), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to stop media gateway session for viewing session {ViewingSessionId}", session.Id);
            }

            await _auditService.LogAsync(
                new Abstractions.Audit.AuditEvent(userId, "VIEWING_SESSION_ENDED", "ViewingSession", session.Id.ToString(), "SUCCESS",
                    Metadata: new { Reason = ViewingEndReason.PARENT_STOPPED.ToString() }),
                cancellationToken);
        }
    }
}
