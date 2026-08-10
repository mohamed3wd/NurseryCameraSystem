using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NurseryCamera.Api.Contracts;
using NurseryCamera.Application.Features.Administration.Dtos;
using NurseryCamera.Application.Features.Administration.Rooms;

namespace NurseryCamera.Api.Controllers;

[ApiController]
[Route("api/admin/rooms")]
[Authorize(Policy = AuthorizationPolicies.NurseryAdmin)]
public sealed class AdminRoomsController : ControllerBase
{
    private readonly ISender _sender;

    public AdminRoomsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<List<RoomDto>>> GetRooms(Guid? nurseryId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetRoomsQuery(nurseryId), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<RoomDto>> CreateRoom(CreateRoomRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new CreateRoomCommand(request.NurseryId, request.Name, request.Code, request.RoomType),
            cancellationToken);

        return Ok(result);
    }
}
