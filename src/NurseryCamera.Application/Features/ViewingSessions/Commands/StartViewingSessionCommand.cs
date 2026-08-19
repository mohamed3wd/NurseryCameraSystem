using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NurseryCamera.Application.Abstractions.Audit;
using NurseryCamera.Application.Abstractions.Identity;
using NurseryCamera.Application.Abstractions.Persistence;
using NurseryCamera.Application.Abstractions.Security;
using NurseryCamera.Application.Abstractions.Streaming;
using NurseryCamera.Application.Abstractions.Time;
using NurseryCamera.Application.Common.Exceptions;
using NurseryCamera.Application.Common.Options;
using NurseryCamera.Application.Features.Cameras.Policies;
using NurseryCamera.Application.Features.ViewingSessions.Dtos;
using NurseryCamera.Domain.Entities;
using NurseryCamera.Domain.Enums;
using NurseryCamera.Domain.Exceptions;

namespace NurseryCamera.Application.Features.ViewingSessions.Commands;

/// <summary>
/// POST /api/children/{childId}/cameras/{cameraId}/viewing-sessions - implements the full
/// authorization + provisioning algorithm from spec sections 8 and 46. Fails closed at every step.
/// </summary>
public sealed record StartViewingSessionCommand(
    Guid ChildId,
    Guid CameraId,
    string ClientType,
    string? DeviceId) : IRequest<StartViewingSessionResponse>;

public sealed class StartViewingSessionCommandValidator : AbstractValidator<StartViewingSessionCommand>
{
    public StartViewingSessionCommandValidator()
    {
        RuleFor(x => x.ChildId).NotEmpty();
        RuleFor(x => x.CameraId).NotEmpty();
        RuleFor(x => x.ClientType).NotEmpty().MaximumLength(50);
        RuleFor(x => x.DeviceId).MaximumLength(200);
    }
}

public sealed class StartViewingSessionCommandHandler : IRequestHandler<StartViewingSessionCommand, StartViewingSessionResponse>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly ICameraAccessPolicy _cameraAccessPolicy;
    private readonly IStreamTokenGenerator _tokenGenerator;
    private readonly ITokenHashService _tokenHashService;
    private readonly ILiveStreamService _liveStreamService;
    private readonly IAuditService _auditService;
    private readonly ViewingPolicyOptions _viewingPolicy;

    public StartViewingSessionCommandHandler(
        IAppDbContext db,
        ICurrentUser currentUser,
        IClock clock,
        ICameraAccessPolicy cameraAccessPolicy,
        IStreamTokenGenerator tokenGenerator,
        ITokenHashService tokenHashService,
        ILiveStreamService liveStreamService,
        IAuditService auditService,
        IOptions<ViewingPolicyOptions> viewingPolicy)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _cameraAccessPolicy = cameraAccessPolicy;
        _tokenGenerator = tokenGenerator;
        _tokenHashService = tokenHashService;
        _liveStreamService = liveStreamService;
        _auditService = auditService;
        _viewingPolicy = viewingPolicy.Value;
    }

    public async Task<StartViewingSessionResponse> Handle(StartViewingSessionCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            throw AppException.AuthenticationRequired();
        }

        var decision = await _cameraAccessPolicy.CanViewAsync(userId, request.ChildId, request.CameraId, cancellationToken);

        if (!decision.Allowed)
        {
            await _auditService.LogAsync(
                new AuditEvent(userId, "CAMERA_VIEW_DENIED", "Camera", request.CameraId.ToString(), "DENIED",
                    Metadata: new { request.ChildId, decision.DenialCode }),
                cancellationToken);

            throw AppException.Forbidden(decision.DenialCode ?? "CAMERA_ACCESS_DENIED", decision.DenialMessage ?? "Camera access was denied.");
        }

        var parentId = decision.ParentId!.Value;
        var attendanceSessionId = decision.AttendanceSessionId!.Value;

        // Concurrent session limits must be enforced with a fresh count immediately before
        // insert; a naive "if count < limit then create" is still subject to a race between
        // concurrent requests, but since EF Core issues the count query and the insert within
        // the same handler/transactional SaveChanges, combined with a unique filtered index at
        // the persistence layer, double-booking is rejected rather than silently allowed.
        var activeParentSessions = await _db.ViewingSessions
            .CountAsync(v => v.ParentId == parentId
                              && (v.Status == ViewingSessionStatus.PENDING || v.Status == ViewingSessionStatus.ACTIVE),
                cancellationToken);

        if (activeParentSessions >= _viewingPolicy.MaxConcurrentSessionsPerParent)
        {
            throw new ViewingLimitReachedException("Maximum concurrent viewing sessions for this parent has been reached.");
        }

        if (_viewingPolicy.MaxConcurrentSessionsPerChild > 0)
        {
            var activeChildSessions = await _db.ViewingSessions
                .CountAsync(v => v.ChildId == request.ChildId
                                  && (v.Status == ViewingSessionStatus.PENDING || v.Status == ViewingSessionStatus.ACTIVE),
                    cancellationToken);

            if (activeChildSessions >= _viewingPolicy.MaxConcurrentSessionsPerChild)
            {
                throw new ViewingLimitReachedException("Maximum concurrent viewing sessions for this child has been reached.");
            }
        }

        var now = _clock.UtcNow;
        var session = new ViewingSession
        {
            Id = Guid.NewGuid(),
            ParentId = parentId,
            ChildId = request.ChildId,
            CameraId = request.CameraId,
            AttendanceSessionId = attendanceSessionId,
            StartedAtUtc = now,
            ExpiresAtUtc = now.AddMinutes(_viewingPolicy.MaxSessionDurationMinutes),
            Status = ViewingSessionStatus.PENDING,
            ClientType = request.ClientType,
            DeviceIdHash = string.IsNullOrWhiteSpace(request.DeviceId) ? null : _tokenHashService.Hash(request.DeviceId)
        };

        var rawToken = _tokenGenerator.Generate();
        var tokenExpiresAtUtc = now.AddSeconds(_viewingPolicy.TokenLifetimeSeconds);
        var streamToken = new StreamToken
        {
            Id = Guid.NewGuid(),
            ViewingSessionId = session.Id,
            TokenHash = _tokenHashService.Hash(rawToken),
            IssuedAtUtc = now,
            ExpiresAtUtc = tokenExpiresAtUtc,
            Status = StreamTokenStatus.ACTIVE
        };

        _db.ViewingSessions.Add(session);
        _db.StreamTokens.Add(streamToken);
        await _db.SaveChangesAsync(cancellationToken);

        // The media gateway call happens outside the persisted PENDING state so a gateway
        // failure can cleanly transition the session to DENIED without leaving an ACTIVE
        // session with no actual stream behind it (spec section 32).
        var startResult = await _liveStreamService.StartAsync(
            new StartStreamRequest(session.Id, request.CameraId, request.ChildId, parentId, request.ClientType, session.ExpiresAtUtc),
            cancellationToken);

        if (!startResult.Success)
        {
            session.Status = ViewingSessionStatus.DENIED;
            session.EndedAtUtc = _clock.UtcNow;
            session.EndReason = ViewingEndReason.CAMERA_OFFLINE;
            streamToken.Status = StreamTokenStatus.REVOKED;
            streamToken.RevokedAtUtc = session.EndedAtUtc;

            await _auditService.LogAsync(
                new AuditEvent(userId, "CAMERA_VIEW_DENIED", "ViewingSession", session.Id.ToString(), "FAILURE",
                    Metadata: new { startResult.FailureCode }),
                cancellationToken);

            throw AppException.Conflict("CAMERA_NOT_AVAILABLE", startResult.FailureMessage ?? "Unable to start the camera stream.");
        }

        session.Status = ViewingSessionStatus.ACTIVE;

        // The terminal status plus all three audit records are flushed by UnitOfWorkBehavior
        // in a single round trip once the handler returns.
        await _auditService.LogAsync(
            new AuditEvent(userId, "CAMERA_VIEW_AUTHORIZED", "ViewingSession", session.Id.ToString(), "SUCCESS"),
            cancellationToken);
        await _auditService.LogAsync(
            new AuditEvent(userId, "VIEWING_SESSION_STARTED", "ViewingSession", session.Id.ToString(), "SUCCESS"),
            cancellationToken);
        await _auditService.LogAsync(
            new AuditEvent(userId, "STREAM_TOKEN_ISSUED", "StreamToken", streamToken.Id.ToString(), "SUCCESS"),
            cancellationToken);

        return new StartViewingSessionResponse(
            session.Id,
            rawToken,
            tokenExpiresAtUtc,
            startResult.MediaProtocol ?? "webrtc",
            startResult.SignalingUrl);
    }
}
