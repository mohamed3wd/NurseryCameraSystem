namespace NurseryCamera.Application.Features.ViewingSessions.Dtos;

/// <summary>The raw <see cref="StreamToken"/> value is returned exactly once and never persisted (spec section 14).</summary>
public sealed record StartViewingSessionResponse(
    Guid SessionId,
    string StreamToken,
    DateTime ExpiresAtUtc,
    string MediaProtocol,
    string? SignalingUrl);
