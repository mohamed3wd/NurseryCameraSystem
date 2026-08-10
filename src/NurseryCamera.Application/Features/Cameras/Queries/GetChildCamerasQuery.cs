using MediatR;
using Microsoft.EntityFrameworkCore;
using NurseryCamera.Application.Abstractions.Identity;
using NurseryCamera.Application.Abstractions.Persistence;
using NurseryCamera.Application.Abstractions.Time;
using NurseryCamera.Application.Common.Exceptions;
using NurseryCamera.Application.Features.Cameras.Dtos;
using NurseryCamera.Domain.Enums;

namespace NurseryCamera.Application.Features.Cameras.Queries;

/// <summary>
/// GET /api/children/{childId}/cameras - spec section 12. The authorization scope is enforced
/// entirely in the server-side query below (never fetch all cameras and filter client-side).
/// Live-only visibility (spec section 12) also requires an active PRESENT attendance session;
/// when the child is not present this returns an empty list rather than an error, matching the
/// parent UI journey ("Live camera unavailable").
/// </summary>
public sealed record GetChildCamerasQuery(Guid ChildId) : IRequest<List<CameraDto>>;

public sealed class GetChildCamerasQueryHandler : IRequestHandler<GetChildCamerasQuery, List<CameraDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public GetChildCamerasQueryHandler(IAppDbContext db, ICurrentUser currentUser, IClock clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<List<CameraDto>> Handle(GetChildCamerasQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            throw AppException.AuthenticationRequired();
        }

        var authorizedChild = await _db.ParentChildren
            .AsNoTracking()
            .Where(pc => pc.Parent.UserId == userId
                         && pc.ChildId == request.ChildId
                         && pc.CanViewCamera
                         && pc.Child.IsActive)
            .Select(pc => new { pc.Child.RoomId })
            .FirstOrDefaultAsync(cancellationToken);

        // A single, uniform "not found" result whether the child does not exist, is not linked
        // to this parent, or camera viewing is disabled - prevents enumeration (BR-013).
        if (authorizedChild is null)
        {
            throw AppException.NotFound("CHILD_NOT_FOUND", "Child not found.");
        }

        if (authorizedChild.RoomId is null)
        {
            return new List<CameraDto>();
        }

        var isPresent = await _db.AttendanceSessions
            .AsNoTracking()
            .AnyAsync(a => a.ChildId == request.ChildId && a.Status == AttendanceStatus.PRESENT, cancellationToken);

        if (!isPresent)
        {
            return new List<CameraDto>();
        }

        var now = _clock.UtcNow;
        var roomId = authorizedChild.RoomId.Value;

        return await _db.CameraRooms
            .AsNoTracking()
            .Where(cr => cr.RoomId == roomId && (cr.ValidToUtc == null || cr.ValidToUtc > now))
            .Select(cr => cr.Camera)
            .Where(c => c.IsActive)
            .Select(c => new CameraDto(
                c.Id,
                c.Name,
                c.Location,
                c.Status.ToString(),
                c.Status == CameraStatus.ACTIVE))
            .ToListAsync(cancellationToken);
    }
}
