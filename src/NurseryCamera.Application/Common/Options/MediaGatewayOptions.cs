namespace NurseryCamera.Application.Common.Options;

/// <summary>
/// Bound from the "MediaGateway" configuration section. See spec section 15/35.
/// Keeps the WebRTC/HLS media server boundary configurable and swappable.
/// </summary>
public sealed class MediaGatewayOptions
{
    public const string SectionName = "MediaGateway";

    /// <summary>Public base URL browsers use for signaling (e.g. http://localhost:8088).</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Shared secret the media gateway presents as X-Media-Gateway-Key.</summary>
    public string? ApiKey { get; set; }

    /// <summary>e.g. "webrtc" or "hls".</summary>
    public string DefaultProtocol { get; set; } = "webrtc";

    /// <summary>"Mock" (default) or "Go2Rtc" for real RTSP→WebRTC via go2rtc.</summary>
    public string Provider { get; set; } = "Mock";

    /// <summary>Internal go2rtc base URL (never exposed to browsers), e.g. http://go2rtc:1984.</summary>
    public string? Go2RtcBaseUrl { get; set; }

    /// <summary>Fallback demo source when a camera RTSP is unavailable (ffmpeg test pattern via go2rtc).</summary>
    public string DemoSource { get; set; } = "ffmpeg:testsrc=size=1280x720:rate=15#video=h264#hardware";
}
