using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NurseryCamera.Api.Contracts;
using NurseryCamera.Application.Features.Administration.Dtos;
using NurseryCamera.Application.Features.Administration.Nurseries;

namespace NurseryCamera.Api.Controllers;

[ApiController]
[Route("api/admin/nurseries")]
[Authorize(Policy = AuthorizationPolicies.NurseryAdmin)]
public sealed class AdminNurseriesController : ControllerBase
{
    private readonly ISender _sender;

    public AdminNurseriesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<List<NurseryDto>>> GetNurseries(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetNurseriesQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<NurseryDto>> CreateNursery(
        CreateNurseryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new CreateNurseryCommand(request.Name, request.TimeZoneId, request.Address),
            cancellationToken);

        return Ok(result);
    }
}
