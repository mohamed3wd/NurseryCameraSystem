using MediatR;
using Microsoft.EntityFrameworkCore;
using NurseryCamera.Application.Abstractions.Identity;
using NurseryCamera.Application.Abstractions.Persistence;
using NurseryCamera.Application.Common.Exceptions;
using NurseryCamera.Application.Features.Parents.Dtos;

namespace NurseryCamera.Application.Features.Parents.Queries;

/// <summary>
/// Returns a single child, scoped to the authenticated parent. Returns the same
/// CHILD_NOT_FOUND error whether the child truly does not exist or simply does not
/// belong to this parent, to prevent enumeration (BR-013).
/// </summary>
public sealed record GetChildQuery(Guid ChildId) : IRequest<ChildDto>;

public sealed class GetChildQueryHandler : IRequestHandler<GetChildQuery, ChildDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetChildQueryHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ChildDto> Handle(GetChildQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            throw AppException.AuthenticationRequired();
        }

        var child = await _db.ParentChildren
            .AsNoTracking()
            .Where(pc => pc.Parent.UserId == userId && pc.ChildId == request.ChildId)
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
            .FirstOrDefaultAsync(cancellationToken);

        return child ?? throw AppException.NotFound("CHILD_NOT_FOUND", "Child not found.");
    }
}
