using MediatR;
using Microsoft.EntityFrameworkCore;
using NurseryCamera.Application.Abstractions.Identity;
using NurseryCamera.Application.Abstractions.Persistence;
using NurseryCamera.Application.Common.Exceptions;
using NurseryCamera.Application.Features.Attendance.Dtos;
using NurseryCamera.Domain.Enums;

namespace NurseryCamera.Application.Features.Attendance.Queries;

/// <summary>Returns the child's current PRESENT attendance session, or null if not present.</summary>
public sealed record GetChildCurrentAttendanceQuery(Guid ChildId) : IRequest<AttendanceDto?>;

public sealed class GetChildCurrentAttendanceQueryHandler : IRequestHandler<GetChildCurrentAttendanceQuery, AttendanceDto?>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetChildCurrentAttendanceQueryHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<AttendanceDto?> Handle(GetChildCurrentAttendanceQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            throw AppException.AuthenticationRequired();
        }

        var isLinkedParent = await _db.ParentChildren
            .AsNoTracking()
            .AnyAsync(pc => pc.ChildId == request.ChildId && pc.Parent.UserId == userId, cancellationToken);

        if (!isLinkedParent)
        {
            var child = await _db.Children.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.ChildId, cancellationToken);
            var isNurseryStaff = child is not null && await _db.Staff
                .AsNoTracking()
                .AnyAsync(s => s.UserId == userId && s.IsActive && s.NurseryId == child.NurseryId, cancellationToken);

            if (!isNurseryStaff)
            {
                throw AppException.NotFound("CHILD_NOT_FOUND", "Child not found.");
            }
        }

        var attendance = await _db.AttendanceSessions
            .AsNoTracking()
            .Where(a => a.ChildId == request.ChildId && a.Status == AttendanceStatus.PRESENT)
            .OrderByDescending(a => a.CheckInUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return attendance is null
            ? null
            : new AttendanceDto(attendance.Id, attendance.ChildId, attendance.StaffId, attendance.CheckInUtc,
                attendance.CheckOutUtc, attendance.Status.ToString(), attendance.Source.ToString(), attendance.Notes);
    }
}
