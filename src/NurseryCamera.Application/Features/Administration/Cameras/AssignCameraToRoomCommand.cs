using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NurseryCamera.Application.Abstractions.Audit;
using NurseryCamera.Application.Abstractions.Identity;
using NurseryCamera.Application.Abstractions.Persistence;
using NurseryCamera.Application.Abstractions.Time;
using NurseryCamera.Application.Common.Exceptions;
using NurseryCamera.Domain.Entities;

namespace NurseryCamera.Application.Features.Administration.Cameras;

public sealed record AssignCameraToRoomCommand(Guid CameraId, Guid RoomId) : IRequest;

public sealed class AssignCameraToRoomCommandValidator : AbstractValidator<AssignCameraToRoomCommand>
{
    public AssignCameraToRoomCommandValidator()
    {
        RuleFor(x => x.CameraId).NotEmpty();
        RuleFor(x => x.RoomId).NotEmpty();
    }
}

public sealed class AssignCameraToRoomCommandHandler : IRequestHandler<AssignCameraToRoomCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly IAuditService _auditService;

    public AssignCameraToRoomCommandHandler(IAppDbContext db, ICurrentUser currentUser, IClock clock, IAuditService auditService)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _auditService = auditService;
    }

    public async Task Handle(AssignCameraToRoomCommand request, CancellationToken cancellationToken)
    {
        var cameraExists = await _db.Cameras.AsNoTracking().AnyAsync(c => c.Id == request.CameraId, cancellationToken);
        if (!cameraExists)
        {
            throw AppException.NotFound("CAMERA_NOT_FOUND", "Camera not found.");
        }

        var roomExists = await _db.Rooms.AsNoTracking().AnyAsync(r => r.Id == request.RoomId, cancellationToken);
        if (!roomExists)
        {
            throw AppException.NotFound("ROOM_NOT_FOUND", "Room not found.");
        }

        var now = _clock.UtcNow;
        var existing = await _db.CameraRooms
            .FirstOrDefaultAsync(cr => cr.CameraId == request.CameraId && cr.RoomId == request.RoomId, cancellationToken);

        if (existing is not null)
        {
            existing.ValidToUtc = null;
        }
        else
        {
            _db.CameraRooms.Add(new CameraRoom
            {
                CameraId = request.CameraId,
                RoomId = request.RoomId,
                ValidFromUtc = now,
                ValidToUtc = null
            });
        }

        await _auditService.LogAsync(
            new AuditEvent(_currentUser.UserId, "CAMERA_ASSIGNMENT_CHANGED", "Camera", request.CameraId.ToString(), "SUCCESS",
                Metadata: new { request.RoomId, Action = "ASSIGNED" }),
            cancellationToken);
    }
}
