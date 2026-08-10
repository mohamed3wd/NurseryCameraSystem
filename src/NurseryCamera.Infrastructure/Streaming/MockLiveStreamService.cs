using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NurseryCamera.Application.Abstractions.Persistence;
using NurseryCamera.Application.Abstractions.Security;
using NurseryCamera.Application.Abstractions.Streaming;
using NurseryCamera.Domain.Enums;

namespace NurseryCamera.Infrastructure.Streaming;

/// <summary>
/// Development/MVP media gateway stand-in. Loads the camera's encrypted secrets and
/// decrypts them only in-memory, only here (as a real WebRTC gateway integration would
/// need to), but never returns RTSP URLs or credentials to the caller. Returns a mock
/// WebRTC signaling payload with protocol "webrtc-mock".
/// </summary>
public sealed class MockLiveStreamService : ILiveStreamService
{
    private readonly IAppDbContext _dbContext;
    private readonly ISecretEncryptionService _encryptionService;
    private readonly ITokenHashService _tokenHashService;
    private readonly ILogger<MockLiveStreamService> _logger;

    public MockLiveStreamService(
        IAppDbContext dbContext,
        ISecretEncryptionService encryptionService,
        ITokenHashService tokenHashService,
        ILogger<MockLiveStreamService> logger)
    {
        _dbContext = dbContext;
        _encryptionService = encryptionService;
        _tokenHashService = tokenHashService;
        _logger = logger;
    }

    public async Task<StartStreamResult> StartAsync(StartStreamRequest request, CancellationToken cancellationToken)
    {
        var camera = await _dbContext.Cameras
            .FirstOrDefaultAsync(c => c.Id == request.CameraId, cancellationToken);

        if (camera is null || !camera.IsActive || camera.Status != CameraStatus.ACTIVE)
        {
            return new StartStreamResult(
                Success: false,
                MediaProtocol: null,
                SignalingUrl: null,
                MediaSessionReference: null,
                FailureCode: "CAMERA_NOT_AVAILABLE",
                FailureMessage: "Camera is not available.");
        }

        // Decrypt only in-memory, only here, only to hand off to a real gateway later.
        // The decrypted values are intentionally not logged, not stored, and not returned.
        try
        {
            _ = _encryptionService.Decrypt(camera.RtspUrlEncrypted);
            _ = _encryptionService.Decrypt(camera.UsernameEncrypted);
            _ = _encryptionService.Decrypt(camera.PasswordEncrypted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt camera secrets for camera {CameraId}.", camera.Id);
            return new StartStreamResult(
                Success: false,
                MediaProtocol: null,
                SignalingUrl: null,
                MediaSessionReference: null,
                FailureCode: "STREAM_AUTHORIZATION_FAILED",
                FailureMessage: "Unable to start the camera stream.");
        }

        var sessionReference = $"mock-session-{request.ViewingSessionId:N}";

        _logger.LogInformation(
            "Mock stream started for ViewingSession {ViewingSessionId} / Camera {CameraId}.",
            request.ViewingSessionId,
            request.CameraId);

        // No real RTSP/WebRTC negotiation happens here; a mock signaling payload is
        // returned so the API/frontend can be built and tested end-to-end.
        return new StartStreamResult(
            Success: true,
            MediaProtocol: "webrtc-mock",
            SignalingUrl: $"mock://media-gateway/{sessionReference}",
            MediaSessionReference: sessionReference);
    }

    public Task StopAsync(StopStreamRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Mock stream stopped for ViewingSession {ViewingSessionId} (media session {MediaSessionReference}).",
            request.ViewingSessionId,
            request.MediaSessionReference ?? "n/a");

        return Task.CompletedTask;
    }

    public async Task<StreamAuthorizationResult> AuthorizeAsync(StreamAuthorizationRequest request, CancellationToken cancellationToken)
    {
        // Full token-design validation chain (spec section 14): token exists, hash matches,
        // not revoked, not expired, session ACTIVE, attendance PRESENT, camera ACTIVE.
        var tokenHash = _tokenHashService.Hash(request.RawStreamToken);

        var token = await _dbContext.StreamTokens
            .Include(t => t.ViewingSession)
                .ThenInclude(v => v.AttendanceSession)
            .Include(t => t.ViewingSession)
                .ThenInclude(v => v.Camera)
            .FirstOrDefaultAsync(
                t => t.ViewingSessionId == request.ViewingSessionId && t.TokenHash == tokenHash,
                cancellationToken);

        if (token is null)
        {
            return Deny("STREAM_TOKEN_NOT_FOUND", "Stream token is invalid.");
        }

        if (token.Status == StreamTokenStatus.REVOKED)
        {
            return Deny("VIEWING_SESSION_REVOKED", "Stream token has been revoked.");
        }

        if (token.Status != StreamTokenStatus.ACTIVE || token.ExpiresAtUtc <= DateTime.UtcNow)
        {
            return Deny("VIEWING_SESSION_EXPIRED", "Stream token has expired.");
        }

        var session = token.ViewingSession;
        if (session.Status != ViewingSessionStatus.ACTIVE)
        {
            return Deny("VIEWING_SESSION_NOT_FOUND", "Viewing session is not active.");
        }

        if (session.AttendanceSession.Status != AttendanceStatus.PRESENT)
        {
            return Deny("CHILD_NOT_PRESENT", "Child is not currently present.");
        }

        if (!session.Camera.IsActive || session.Camera.Status != CameraStatus.ACTIVE)
        {
            return Deny("CAMERA_NOT_AVAILABLE", "Camera is not available.");
        }

        return new StreamAuthorizationResult(Authorized: true);
    }

    private static StreamAuthorizationResult Deny(string code, string message)
        => new(Authorized: false, DenialCode: code, DenialMessage: message);
}
