using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NurseryCamera.Api.Contracts;
using NurseryCamera.Api.Filters;
using NurseryCamera.Application.Abstractions.Streaming;
using NurseryCamera.Infrastructure.Streaming;

namespace NurseryCamera.Api.Controllers;

/// <summary>
/// Machine-to-machine endpoints the media gateway calls before admitting a viewer
/// (spec section 14/15). Protected by a shared API key — never JWT / never parent-facing.
/// </summary>
[ApiController]
[Route("api/internal/stream")]
[AllowAnonymous]
[MediaGatewayApiKey]
public sealed class StreamAuthorizationController : ControllerBase
{
    private readonly ILiveStreamService _liveStreamService;
    private readonly IStreamSourceResolver _streamSourceResolver;

    public StreamAuthorizationController(
        ILiveStreamService liveStreamService,
        IStreamSourceResolver streamSourceResolver)
    {
        _liveStreamService = liveStreamService;
        _streamSourceResolver = streamSourceResolver;
    }

    [HttpPost("authorize")]
    public async Task<ActionResult<StreamAuthorizeResponse>> Authorize(
        StreamAuthorizeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _liveStreamService.AuthorizeAsync(
            new StreamAuthorizationRequest(request.ViewingSessionId, request.StreamToken),
            cancellationToken);

        return Ok(new StreamAuthorizeResponse(result.Authorized, result.DenialCode, result.DenialMessage));
    }

    /// <summary>
    /// Returns a private media source URL for go2rtc after token validation.
    /// Response must never be forwarded to browsers.
    /// </summary>
    [HttpPost("resolve")]
    public async Task<ActionResult<StreamResolveResponse>> Resolve(
        StreamAuthorizeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _streamSourceResolver.ResolveAsync(
            request.ViewingSessionId,
            request.StreamToken,
            cancellationToken);

        if (!result.Authorized)
        {
            return Ok(new StreamResolveResponse(false, null, null, result.DenialCode, result.DenialMessage));
        }

        return Ok(new StreamResolveResponse(true, result.StreamName, result.SourceUrl, null, null));
    }
}
