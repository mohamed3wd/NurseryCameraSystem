using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NurseryCamera.Api.Contracts;
using NurseryCamera.Application.Features.Attendance.Commands;
using NurseryCamera.Application.Features.Attendance.Dtos;
using NurseryCamera.Application.Features.Attendance.Queries;

namespace NurseryCamera.Api.Controllers;

[ApiController]
[Route("api/children/{childId:guid}/attendance")]
[Authorize]
public sealed class AttendanceController : ControllerBase
{
    private readonly ISender _sender;

    public AttendanceController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("current")]
    public async Task<ActionResult<AttendanceDto?>> GetCurrent(Guid childId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetChildCurrentAttendanceQuery(childId), cancellationToken);
        return Ok(result);
    }

    [HttpPost("check-in")]
    [Authorize(Policy = AuthorizationPolicies.AttendanceManager)]
    public async Task<ActionResult<AttendanceDto>> CheckIn(Guid childId, CheckInRequest? request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new CheckInChildCommand(childId, request?.Notes), cancellationToken);
        return Ok(result);
    }

    [HttpPost("check-out")]
    [Authorize(Policy = AuthorizationPolicies.AttendanceManager)]
    public async Task<ActionResult<AttendanceDto>> CheckOut(Guid childId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new CheckOutChildCommand(childId), cancellationToken);
        return Ok(result);
    }
}
