using MediatR;
using Microsoft.EntityFrameworkCore;
using NurseryCamera.Application.Abstractions.Persistence;
using NurseryCamera.Application.Features.Administration.Dtos;

namespace NurseryCamera.Application.Features.Administration.Cameras;

/// <summary>Admin listing of cameras (optionally filtered by nursery). Never includes secrets.</summary>
public sealed record GetCamerasQuery(Guid? NurseryId) : IRequest<List<CameraAdminDto>>;

public sealed class GetCamerasQueryHandler : IRequestHandler<GetCamerasQuery, List<CameraAdminDto>>
{
    private readonly IAppDbContext _db;

    public GetCamerasQueryHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<List<CameraAdminDto>> Handle(GetCamerasQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Cameras.AsNoTracking();

        if (request.NurseryId is { } nurseryId)
        {
            query = query.Where(c => c.NurseryId == nurseryId);
        }

        return await query
            .Select(c => new CameraAdminDto(
                c.Id,
                c.NurseryId,
                c.Name,
                c.Location,
                c.Status.ToString(),
                c.StreamProfile,
                c.IsActive,
                c.LastHealthCheckUtc,
                c.CameraRooms.Where(cr => cr.ValidToUtc == null).Select(cr => cr.RoomId).ToList()))
            .ToListAsync(cancellationToken);
    }
}
