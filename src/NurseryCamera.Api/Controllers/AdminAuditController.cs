using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NurseryCamera.Application.Common.Models;
using NurseryCamera.Application.Features.Administration.Audit;
using NurseryCamera.Application.Features.Administration.Dtos;

namespace NurseryCamera.Api.Controllers;

[ApiController]
[Route("api/admin/audit-logs")]
[Authorize(Policy = AuthorizationPolicies.AuditViewer)]
public sealed class AdminAuditController : ControllerBase
{
    private readonly ISender _sender;

    public AdminAuditController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> GetAuditLogs(
        DateTime? fromUtc,
        DateTime? toUtc,
        string? action,
        Guid? userId,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetAuditLogsQuery(fromUtc, toUtc, action, userId, page, pageSize), cancellationToken);
        return Ok(result);
    }
}
