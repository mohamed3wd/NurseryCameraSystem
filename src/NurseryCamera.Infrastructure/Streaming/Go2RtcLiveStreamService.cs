using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NurseryCamera.Application.Abstractions.Persistence;
using NurseryCamera.Application.Abstractions.Security;
using NurseryCamera.Application.Abstractions.Streaming;
using NurseryCamera.Application.Abstractions.Time;
using NurseryCamera.Application.Common.Options;
using NurseryCamera.Domain.Enums;

namespace NurseryCamera.Infrastructure.Streaming;

/// <summary>
/// Real media-gateway boundary: decrypts camera RTSP only in-memory, never returns it to
/// clients, and hands the browser a public WebRTC signaling URL on the media gateway.
/// Actual RTSP ingest / WebRTC negotiation is performed by go2rtc behind that gateway.
/// </summary>
public sealed class Go2RtcLiveStreamService : ILiveStreamService
{
    private readonly IAppDbContext _dbContext;
    private readonly ISecretEncryptionService _encryptionService;
    private readonly ITokenHashService _tokenHashService;
    private readonly IClock _clock;
    private readonly MediaGatewayOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<Go2RtcLiveStreamService> _logger;

    public Go2RtcLiveStreamService(
        IAppDbContext dbContext,
        ISecretEncryptionService encryptionService,
        ITokenHashService tokenHashService,
        IClock clock,
        IOptions<MediaGatewayOptions> options,
        IHttpClientFactory httpClientFactory,
        ILogger<Go2RtcLiveStreamService> logger)
    {
        _dbContext = dbContext;
        _encryptionService = encryptionService;
        _tokenHashService = tokenHashService;
        _clock = clock;
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<StartStreamResult> StartAsync(StartStreamRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            return Fail("MEDIA_GATEWAY_NOT_CONFIGURED", "Media gateway public base URL is not configured.");
        }

        var camera = await _dbContext.Cameras
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CameraId, cancellationToken);

        if (camera is null || !camera.IsActive || camera.Status != CameraStatus.ACTIVE)
        {
            return Fail("CAMERA_NOT_AVAILABLE", "Camera is not available.");
        }

        string source;
        try
        {
            source = ResolveSource(camera.RtspUrlEncrypted, camera.UsernameEncrypted, camera.PasswordEncrypted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to prepare stream source for camera {CameraId}.", camera.Id);
            return Fail("STREAM_AUTHORIZATION_FAILED", "Unable to start the camera stream.");
        }

        var streamName = $"vs_{request.ViewingSessionId:N}";
        var go2RtcBase = (_options.Go2RtcBaseUrl ?? string.Empty).TrimEnd('/');

        if (!string.IsNullOrWhiteSpace(go2RtcBase))
        {
            try
            {
                await RegisterGo2RtcStreamAsync(go2RtcBase, streamName, source, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "go2rtc registration failed for ViewingSession {ViewingSessionId}; viewer may fall back to demo source.",
                    request.ViewingSessionId);
            }
        }

        var publicBase = _options.BaseUrl.TrimEnd('/');
        var signalingUrl =
            $"{publicBase}/viewer/webrtc?sessionId={request.ViewingSessionId:D}";

        _logger.LogInformation(
            "WebRTC stream prepared for ViewingSession {ViewingSessionId} / Camera {CameraId}.",
            request.ViewingSessionId,
            request.CameraId);

        return new StartStreamResult(
            Success: true,
            MediaProtocol: _options.DefaultProtocol,
            SignalingUrl: signalingUrl,
            MediaSessionReference: streamName);
    }

    public async Task StopAsync(StopStreamRequest request, CancellationToken cancellationToken)
    {
        var go2RtcBase = (_options.Go2RtcBaseUrl ?? string.Empty).TrimEnd('/');
        var streamName = request.MediaSessionReference ?? $"vs_{request.ViewingSessionId:N}";

        if (string.IsNullOrWhiteSpace(go2RtcBase))
        {
            return;
        }

        try
        {
            var client = _httpClientFactory.CreateClient("go2rtc");
            // Best-effort cleanup; go2rtc removes dynamic streams when unused.
            await client.DeleteAsync($"{go2RtcBase}/api/streams?src={Uri.EscapeDataString(streamName)}", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "go2rtc stream cleanup failed for {StreamName}.", streamName);
        }
    }

    public async Task<StreamAuthorizationResult> AuthorizeAsync(StreamAuthorizationRequest request, CancellationToken cancellationToken)
    {
        var tokenHash = _tokenHashService.Hash(request.RawStreamToken);

        var facts = await StreamTokenAuthorizer.LoadFactsAsync(
            _dbContext, request.ViewingSessionId, tokenHash, cancellationToken);

        return StreamTokenAuthorizer.Evaluate(facts, _clock.UtcNow);
    }

    private string ResolveSource(string rtspEncrypted, string usernameEncrypted, string passwordEncrypted)
    {
        var rtsp = _encryptionService.Decrypt(rtspEncrypted);
        if (string.IsNullOrWhiteSpace(rtsp) ||
            rtsp.StartsWith("demo://", StringComparison.OrdinalIgnoreCase) ||
            rtsp.Contains("192.0.2.", StringComparison.Ordinal)) // RFC 5737 documentation range used by seeder
        {
            return _options.DemoSource;
        }

        var username = _encryptionService.Decrypt(usernameEncrypted);
        var password = _encryptionService.Decrypt(passwordEncrypted);

        if (!string.IsNullOrWhiteSpace(username) &&
            rtsp.StartsWith("rtsp://", StringComparison.OrdinalIgnoreCase) &&
            !rtsp.Contains('@'))
        {
            var rest = rtsp["rtsp://".Length..];
            return $"rtsp://{Uri.EscapeDataString(username)}:{Uri.EscapeDataString(password)}@{rest}";
        }

        return rtsp;
    }

    private async Task RegisterGo2RtcStreamAsync(string go2RtcBase, string streamName, string source, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("go2rtc");
        var url = $"{go2RtcBase}/api/streams?name={Uri.EscapeDataString(streamName)}&src={Uri.EscapeDataString(source)}";
        using var response = await client.PutAsync(url, content: null, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private static StartStreamResult Fail(string code, string message) =>
        new(false, null, null, null, code, message);
}
