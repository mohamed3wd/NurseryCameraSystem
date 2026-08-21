using MediatR;
using Microsoft.EntityFrameworkCore;
using NurseryCamera.Application.Abstractions.Persistence;
using NurseryCamera.Application.Features.Administration.Dtos;

namespace NurseryCamera.Application.Features.Administration.Nurseries;

public sealed record GetNurseriesQuery : IRequest<List<NurseryDto>>;

public sealed class GetNurseriesQueryHandler : IRequestHandler<GetNurseriesQuery, List<NurseryDto>>
{
    private readonly IAppDbContext _db;

    public GetNurseriesQueryHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<List<NurseryDto>> Handle(GetNurseriesQuery request, CancellationToken cancellationToken)
    {
        return await _db.Nurseries
            .AsNoTracking()
            .OrderBy(n => n.Name)
            .Select(n => new NurseryDto(n.Id, n.Name, n.TimeZoneId, n.Address, n.IsActive))
            .ToListAsync(cancellationToken);
    }
}
