using MediatR;
using Microsoft.EntityFrameworkCore;
using NurseryCamera.Application.Abstractions.Persistence;
using NurseryCamera.Application.Features.Administration.Dtos;

namespace NurseryCamera.Application.Features.Administration.Rooms;

public sealed record GetRoomsQuery(Guid? NurseryId) : IRequest<List<RoomDto>>;

public sealed class GetRoomsQueryHandler : IRequestHandler<GetRoomsQuery, List<RoomDto>>
{
    private readonly IAppDbContext _db;

    public GetRoomsQueryHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<List<RoomDto>> Handle(GetRoomsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Rooms.AsNoTracking();

        if (request.NurseryId is { } nurseryId)
        {
            query = query.Where(r => r.NurseryId == nurseryId);
        }

        return await query
            .Select(r => new RoomDto(r.Id, r.NurseryId, r.Name, r.Code, r.RoomType, r.IsActive))
            .ToListAsync(cancellationToken);
    }
}
