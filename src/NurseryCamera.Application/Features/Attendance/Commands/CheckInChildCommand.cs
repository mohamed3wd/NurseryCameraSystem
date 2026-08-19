using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NurseryCamera.Application.Abstractions.Audit;
using NurseryCamera.Application.Abstractions.Identity;
using NurseryCamera.Application.Abstractions.Notifications;
using NurseryCamera.Application.Abstractions.Persistence;
using NurseryCamera.Application.Abstractions.Time;
using NurseryCamera.Application.Common.Exceptions;
using NurseryCamera.Application.Features.Attendance.Dtos;
using NurseryCamera.Domain.Entities;
using NurseryCamera.Domain.Enums;

namespace NurseryCamera.Application.Features.Attendance.Commands;

/// <summary>Staff-only. See spec section 9 (Check-In Flow).</summary>
public sealed record CheckInChildCommand(Guid ChildId, string? Notes) : IRequest<AttendanceDto>;

public sealed class CheckInChildCommandValidator : AbstractValidator<CheckInChildCommand>
{
    public CheckInChildCommandValidator()
    {
        RuleFor(x => x.ChildId).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public sealed class CheckInChildCommandHandler : IRequestHandler<CheckInChildCommand, AttendanceDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly IAuditService _auditService;
    private readonly INotificationService _notificationService;

    public CheckInChildCommandHandler(
        IAppDbContext db,
        ICurrentUser currentUser,
        IClock clock,
        IAuditService auditService,
        INotificationService notificationService)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _auditService = auditService;
        _notificationService = notificationService;
    }

    public async Task<AttendanceDto> Handle(CheckInChildCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            throw AppException.AuthenticationRequired();
        }

        var staff = await _db.Staff
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId && s.IsActive, cancellationToken);

        if (staff is null)
        {
            await _auditService.LogAsync(
                new AuditEvent(userId, "CHILD_CHECK_IN", "Child", request.ChildId.ToString(), "FAILURE"),
                cancellationToken);
            throw AppException.Forbidden(message: "Only active staff can check in a child.");
        }

        var child = await _db.Children.FirstOrDefaultAsync(c => c.Id == request.ChildId && c.IsActive, cancellationToken);
        if (child is null)
        {
            throw AppException.NotFound("CHILD_NOT_FOUND", "Child not found.");
        }

        var existing = await _db.AttendanceSessions
            .FirstOrDefaultAsync(a => a.ChildId == request.ChildId && a.Status == AttendanceStatus.PRESENT, cancellationToken);

        if (existing is not null)
        {
            return ToDto(existing);
        }

        var now = _clock.UtcNow;
        var attendance = new AttendanceSession
        {
            Id = Guid.NewGuid(),
            ChildId = request.ChildId,
            StaffId = staff.Id,
            CheckInUtc = now,
            Status = AttendanceStatus.PRESENT,
            Source = AttendanceSource.Manual,
            Notes = request.Notes
        };

        _db.AttendanceSessions.Add(attendance);

        await _auditService.LogAsync(
            new AuditEvent(userId, "CHILD_CHECK_IN", "AttendanceSession", attendance.Id.ToString(), "SUCCESS",
                Metadata: new { attendance.ChildId, attendance.StaffId, attendance.Source }),
            cancellationToken);

        var parentUserIds = await _db.ParentChildren
            .AsNoTracking()
            .Where(pc => pc.ChildId == request.ChildId)
            .Select(pc => pc.Parent.UserId)
            .ToListAsync(cancellationToken);

        await _notificationService.NotifyChildCheckedInAsync(request.ChildId, parentUserIds, now, cancellationToken);

        return ToDto(attendance);
    }

    private static AttendanceDto ToDto(AttendanceSession a) => new(
        a.Id, a.ChildId, a.StaffId, a.CheckInUtc, a.CheckOutUtc, a.Status.ToString(), a.Source.ToString(), a.Notes);
}
