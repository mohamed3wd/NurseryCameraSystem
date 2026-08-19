using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NurseryCamera.Application.Abstractions.Persistence;
using NurseryCamera.Application.Abstractions.Security;
using NurseryCamera.Application.Abstractions.Streaming;
using NurseryCamera.Application.Abstractions.Time;
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
    private readonly IClock _clock;
    private readonly ILogger<MockLiveStreamService> _logger;

    public MockLiveStreamService(
        IAppDbContext dbContext,
        ISecretEncryptionService encryptionService,
        ITokenHashService tokenHashService,
        IClock clock,
        ILogger<MockLiveStreamService> logger)
    {
        _dbContext = dbContext;
        _encryptionService = encryptionService;
        _tokenHashService = tokenHashService;
        _clock = clock;
        _logger = logger;
    }

    public async Task<StartStreamResult> StartAsync(StartStreamRequest request, CancellationToken cancellationToken)
    {
        var camera = await _dbContext.Cameras
            .AsNoTracking()
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
        var tokenHash = _tokenHashService.Hash(request.RawStreamToken);

        var facts = await StreamTokenAuthorizer.LoadFactsAsync(
            _dbContext, request.ViewingSessionId, tokenHash, cancellationToken);

        return StreamTokenAuthorizer.Evaluate(facts, _clock.UtcNow);
    }
}
