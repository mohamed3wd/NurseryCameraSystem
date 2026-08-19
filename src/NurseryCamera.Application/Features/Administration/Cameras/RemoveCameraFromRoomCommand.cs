using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NurseryCamera.Application.Abstractions.Audit;
using NurseryCamera.Application.Abstractions.Identity;
using NurseryCamera.Application.Abstractions.Persistence;
using NurseryCamera.Application.Abstractions.Time;
using NurseryCamera.Application.Common.Exceptions;

namespace NurseryCamera.Application.Features.Administration.Cameras;

public sealed record RemoveCameraFromRoomCommand(Guid CameraId, Guid RoomId) : IRequest;

public sealed class RemoveCameraFromRoomCommandValidator : AbstractValidator<RemoveCameraFromRoomCommand>
{
    public RemoveCameraFromRoomCommandValidator()
    {
        RuleFor(x => x.CameraId).NotEmpty();
        RuleFor(x => x.RoomId).NotEmpty();
    }
}

public sealed class RemoveCameraFromRoomCommandHandler : IRequestHandler<RemoveCameraFromRoomCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly IAuditService _auditService;

    public RemoveCameraFromRoomCommandHandler(IAppDbContext db, ICurrentUser currentUser, IClock clock, IAuditService auditService)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _auditService = auditService;
    }

    public async Task Handle(RemoveCameraFromRoomCommand request, CancellationToken cancellationToken)
    {
        var assignment = await _db.CameraRooms
            .FirstOrDefaultAsync(cr => cr.CameraId == request.CameraId && cr.RoomId == request.RoomId && cr.ValidToUtc == null, cancellationToken);

        if (assignment is null)
        {
            throw AppException.NotFound("CAMERA_ASSIGNMENT_NOT_FOUND", "Camera is not currently assigned to this room.");
        }

        assignment.ValidToUtc = _clock.UtcNow;

        await _auditService.LogAsync(
            new AuditEvent(_currentUser.UserId, "CAMERA_ASSIGNMENT_CHANGED", "Camera", request.CameraId.ToString(), "SUCCESS",
                Metadata: new { request.RoomId, Action = "REMOVED" }),
            cancellationToken);
    }
}
