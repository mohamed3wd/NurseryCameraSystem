using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NurseryCamera.Application.Features.ViewingSessions.Commands;
using NurseryCamera.Application.Features.ViewingSessions.Dtos;
using NurseryCamera.Application.Features.ViewingSessions.Queries;

namespace NurseryCamera.Api.Controllers;

[ApiController]
[Route("api/viewing-sessions")]
[Authorize(Policy = AuthorizationPolicies.ParentOnly)]
public sealed class ViewingSessionsController : ControllerBase
{
    private readonly ISender _sender;

    public ViewingSessionsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("{sessionId:guid}")]
    public async Task<ActionResult<ViewingSessionDto>> Get(Guid sessionId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetViewingSessionQuery(sessionId), cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{sessionId:guid}")]
    public async Task<IActionResult> Stop(Guid sessionId, CancellationToken cancellationToken)
    {
        await _sender.Send(new StopViewingSessionCommand(sessionId), cancellationToken);
        return NoContent();
    }
}
