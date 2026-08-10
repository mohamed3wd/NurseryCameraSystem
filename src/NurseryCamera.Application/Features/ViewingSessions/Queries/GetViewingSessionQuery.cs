using MediatR;
using Microsoft.EntityFrameworkCore;
using NurseryCamera.Application.Abstractions.Identity;
using NurseryCamera.Application.Abstractions.Persistence;
using NurseryCamera.Application.Common.Exceptions;
using NurseryCamera.Application.Features.ViewingSessions.Dtos;

namespace NurseryCamera.Application.Features.ViewingSessions.Queries;

public sealed record GetViewingSessionQuery(Guid SessionId) : IRequest<ViewingSessionDto>;

public sealed class GetViewingSessionQueryHandler : IRequestHandler<GetViewingSessionQuery, ViewingSessionDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetViewingSessionQueryHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ViewingSessionDto> Handle(GetViewingSessionQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            throw AppException.AuthenticationRequired();
        }

        // IDOR-safe: only returns a session that belongs to the requesting parent (spec section 38).
        var dto = await _db.ViewingSessions
            .AsNoTracking()
            .Where(v => v.Id == request.SessionId && v.Parent.UserId == userId)
            .Select(v => new ViewingSessionDto(
                v.Id, v.ChildId, v.CameraId, v.Status.ToString(), v.StartedAtUtc, v.ExpiresAtUtc, v.EndedAtUtc,
                v.EndReason == null ? null : v.EndReason.ToString()))
            .FirstOrDefaultAsync(cancellationToken);

        return dto ?? throw AppException.NotFound("VIEWING_SESSION_NOT_FOUND", "Viewing session not found.");
    }
}
