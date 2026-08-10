using MediatR;
using Microsoft.EntityFrameworkCore;
using NurseryCamera.Application.Abstractions.Identity;
using NurseryCamera.Application.Abstractions.Persistence;
using NurseryCamera.Application.Common.Exceptions;
using NurseryCamera.Application.Features.Parents.Dtos;

namespace NurseryCamera.Application.Features.Parents.Queries;

/// <summary>Returns only the children linked to the authenticated parent (BR-003).</summary>
public sealed record GetParentChildrenQuery : IRequest<List<ChildDto>>;

public sealed class GetParentChildrenQueryHandler : IRequestHandler<GetParentChildrenQuery, List<ChildDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetParentChildrenQueryHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<List<ChildDto>> Handle(GetParentChildrenQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            throw AppException.AuthenticationRequired();
        }

        return await _db.ParentChildren
            .AsNoTracking()
            .Where(pc => pc.Parent.UserId == userId)
            .Select(pc => new ChildDto(
                pc.Child.Id,
                pc.Child.FirstName,
                pc.Child.LastName,
                pc.Child.DateOfBirth,
                pc.Child.RoomId,
                pc.Child.Room != null ? pc.Child.Room.Name : null,
                pc.Child.EnrollmentStatus.ToString(),
                pc.Child.IsActive,
                pc.CanViewCamera))
            .ToListAsync(cancellationToken);
    }
}
