namespace NurseryCamera.Api.Contracts;

/// <summary>Request body the media gateway sends to validate an in-flight stream token (spec section 14/15).</summary>
public sealed record StreamAuthorizeRequest(Guid ViewingSessionId, string StreamToken);

public sealed record StreamAuthorizeResponse(bool Authorized, string? DenialCode, string? DenialMessage);

/// <summary>Private resolve payload for the media gateway only — never return to parents.</summary>
public sealed record StreamResolveResponse(
    bool Authorized,
    string? StreamName,
    string? SourceUrl,
    string? DenialCode,
    string? DenialMessage);
