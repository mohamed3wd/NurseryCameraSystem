using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NurseryCamera.Application.Abstractions.Persistence;
using NurseryCamera.Application.Common.Models;
using NurseryCamera.Application.Features.Administration.Dtos;

namespace NurseryCamera.Application.Features.Administration.Audit;

public sealed record GetAuditLogsQuery(
    DateTime? FromUtc,
    DateTime? ToUtc,
    string? Action,
    Guid? UserId,
    int Page = 1,
    int PageSize = 50) : IRequest<PagedResult<AuditLogDto>>;

public sealed class GetAuditLogsQueryValidator : AbstractValidator<GetAuditLogsQuery>
{
    public GetAuditLogsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}

public sealed class GetAuditLogsQueryHandler : IRequestHandler<GetAuditLogsQuery, PagedResult<AuditLogDto>>
{
    private readonly IAppDbContext _db;

    public GetAuditLogsQueryHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<AuditLogDto>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.AuditLogs.AsNoTracking().AsQueryable();

        if (request.FromUtc is { } from)
        {
            query = query.Where(a => a.CreatedAtUtc >= from);
        }

        if (request.ToUtc is { } to)
        {
            query = query.Where(a => a.CreatedAtUtc <= to);
        }

        if (!string.IsNullOrWhiteSpace(request.Action))
        {
            query = query.Where(a => a.Action == request.Action);
        }

        if (request.UserId is { } userId)
        {
            query = query.Where(a => a.UserId == userId);
        }

        query = query.OrderByDescending(a => a.CreatedAtUtc);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new AuditLogDto(a.Id, a.UserId, a.Action, a.EntityType, a.EntityId, a.Result, a.CreatedAtUtc, a.MetadataJson))
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditLogDto>(items, totalCount, request.Page, request.PageSize);
    }
}
