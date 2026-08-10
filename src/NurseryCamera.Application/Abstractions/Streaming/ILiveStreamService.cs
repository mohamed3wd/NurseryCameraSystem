namespace NurseryCamera.Application.Abstractions.Streaming;

/// <summary>
/// Boundary between the domain/application layer and the concrete media server
/// (WebRTC/HLS gateway). See spec section 15. Implementations may be swapped
/// (WebRtcMediaGateway, HlsMediaGateway, MockMediaGateway) without touching this contract.
/// Never expose RTSP URLs or camera credentials through any of these members.
/// </summary>
public interface ILiveStreamService
{
    Task<StartStreamResult> StartAsync(StartStreamRequest request, CancellationToken cancellationToken);

    Task StopAsync(StopStreamRequest request, CancellationToken cancellationToken);

    Task<StreamAuthorizationResult> AuthorizeAsync(StreamAuthorizationRequest request, CancellationToken cancellationToken);
}

public sealed record StartStreamRequest(
    Guid ViewingSessionId,
    Guid CameraId,
    Guid ChildId,
    Guid ParentId,
    string ClientType,
    DateTime ExpiresAtUtc);

public sealed record StartStreamResult(
    bool Success,
    string? MediaProtocol,
    string? SignalingUrl,
    string? MediaSessionReference,
    string? FailureCode = null,
    string? FailureMessage = null);

public sealed record StopStreamRequest(
    Guid ViewingSessionId,
    string? MediaSessionReference);

public sealed record StreamAuthorizationRequest(
    Guid ViewingSessionId,
    string RawStreamToken);

public sealed record StreamAuthorizationResult(
    bool Authorized,
    string? DenialCode = null,
    string? DenialMessage = null);
