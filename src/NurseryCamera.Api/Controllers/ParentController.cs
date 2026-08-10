using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NurseryCamera.Application.Features.Auth.Dtos;
using NurseryCamera.Application.Features.Auth.Queries;
using NurseryCamera.Application.Features.Parents.Dtos;
using NurseryCamera.Application.Features.Parents.Queries;

namespace NurseryCamera.Api.Controllers;

[ApiController]
[Route("api/parent")]
[Authorize(Policy = AuthorizationPolicies.ParentOnly)]
public sealed class ParentController : ControllerBase
{
    private readonly ISender _sender;

    public ParentController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("profile")]
    public async Task<ActionResult<UserDto>> GetProfile(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetCurrentUserQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("children")]
    public async Task<ActionResult<List<ChildDto>>> GetChildren(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetParentChildrenQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("children/{childId:guid}")]
    public async Task<ActionResult<ChildDto>> GetChild(Guid childId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetChildQuery(childId), cancellationToken);
        return Ok(result);
    }
}
