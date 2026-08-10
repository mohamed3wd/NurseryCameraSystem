using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NurseryCamera.Api.Contracts;
using NurseryCamera.Application.Features.Administration.Cameras;
using NurseryCamera.Application.Features.Administration.Dtos;

namespace NurseryCamera.Api.Controllers;

[ApiController]
[Route("api/admin/cameras")]
[Authorize(Policy = AuthorizationPolicies.CameraManager)]
public sealed class AdminCamerasController : ControllerBase
{
    private readonly ISender _sender;

    public AdminCamerasController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<List<CameraAdminDto>>> GetCameras(Guid? nurseryId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetCamerasQuery(nurseryId), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<CameraAdminDto>> CreateCamera(CreateCameraRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new CreateCameraCommand(
                request.NurseryId,
                request.Name,
                request.Location,
                request.RtspUrl,
                request.Username,
                request.Password,
                request.StreamProfile),
            cancellationToken);

        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CameraAdminDto>> UpdateCamera(Guid id, UpdateCameraRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new UpdateCameraCommand(
                id,
                request.Name,
                request.Location,
                request.RtspUrl,
                request.Username,
                request.Password,
                request.StreamProfile),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("{id:guid}/enable")]
    public async Task<IActionResult> EnableCamera(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new EnableCameraCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/disable")]
    public async Task<IActionResult> DisableCamera(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DisableCameraCommand(id), cancellationToken);
        return NoContent();
    }

    // Routed at /api/admin/rooms/{roomId}/cameras/{cameraId} rather than under this
    // controller's own /api/admin/cameras prefix, matching the room-scoped assignment URL.
    [HttpPost("~/api/admin/rooms/{roomId:guid}/cameras/{cameraId:guid}")]
    public async Task<IActionResult> AssignCameraToRoom(Guid roomId, Guid cameraId, CancellationToken cancellationToken)
    {
        await _sender.Send(new AssignCameraToRoomCommand(cameraId, roomId), cancellationToken);
        return NoContent();
    }

    [HttpDelete("~/api/admin/rooms/{roomId:guid}/cameras/{cameraId:guid}")]
    public async Task<IActionResult> RemoveCameraFromRoom(Guid roomId, Guid cameraId, CancellationToken cancellationToken)
    {
        await _sender.Send(new RemoveCameraFromRoomCommand(cameraId, roomId), cancellationToken);
        return NoContent();
    }
}
