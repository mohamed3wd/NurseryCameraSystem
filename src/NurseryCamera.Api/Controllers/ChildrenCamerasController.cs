using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NurseryCamera.Api.Contracts;
using NurseryCamera.Application.Features.Cameras.Dtos;
using NurseryCamera.Application.Features.Cameras.Queries;
using NurseryCamera.Application.Features.ViewingSessions.Commands;
using NurseryCamera.Application.Features.ViewingSessions.Dtos;

namespace NurseryCamera.Api.Controllers;

[ApiController]
[Route("api/children/{childId:guid}/cameras")]
[Authorize(Policy = AuthorizationPolicies.ParentOnly)]
public sealed class ChildrenCamerasController : ControllerBase
{
    private readonly ISender _sender;

    public ChildrenCamerasController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<List<CameraDto>>> GetCameras(Guid childId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetChildCamerasQuery(childId), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{cameraId:guid}/viewing-sessions")]
    public async Task<ActionResult<StartViewingSessionResponse>> StartViewingSession(
        Guid childId,
        Guid cameraId,
        StartViewingSessionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new StartViewingSessionCommand(childId, cameraId, request.ClientType, request.DeviceId),
            cancellationToken);

        return Ok(result);
    }
}
