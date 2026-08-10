using Microsoft.EntityFrameworkCore;
using NurseryCamera.Application.Abstractions.Persistence;
using NurseryCamera.Application.Abstractions.Time;
using NurseryCamera.Domain.Enums;

namespace NurseryCamera.Application.Features.Cameras.Policies;

/// <summary>
/// Implements the full parent camera authorization algorithm from spec section 8, steps 1-14.
/// (Concurrent session limits - step 15 - are enforced by the StartViewingSessionCommand
/// handler, since that is a per-request-rate concern rather than a viewing scope decision.)
/// Every branch below returns a denial before any data is disclosed, so a caller can never
/// distinguish "child not found" from "child belongs to someone else" (BR-013).
/// </summary>
public sealed class CameraAccessPolicy : ICameraAccessPolicy
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;

    public CameraAccessPolicy(IAppDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<CameraAccessDecision> CanViewAsync(Guid userId, Guid childId, Guid cameraId, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;

        var parent = await _db.Parents
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (parent is null || parent.Status != ParentStatus.Active)
        {
            return Denied("FORBIDDEN", "No active parent profile was found for this account.");
        }

        var relation = await _db.ParentChildren
            .AsNoTracking()
            .FirstOrDefaultAsync(pc => pc.ParentId == parent.Id && pc.ChildId == childId, cancellationToken);

        if (relation is null)
        {
            return Denied("PARENT_CHILD_RELATION_NOT_FOUND", "This child is not linked to your account.");
        }

        if (!relation.CanViewCamera)
        {
            return Denied("CAMERA_ACCESS_DENIED", "Camera viewing has not been enabled for this child.");
        }

        var child = await _db.Children
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == childId, cancellationToken);

        if (child is null || !child.IsActive)
        {
            return Denied("CHILD_NOT_FOUND", "Child not found.");
        }

        if (child.RoomId is null)
        {
            return Denied("CAMERA_ACCESS_DENIED", "Child is not currently assigned to a room.");
        }

        var attendance = await _db.AttendanceSessions
            .AsNoTracking()
            .Where(a => a.ChildId == childId && a.Status == AttendanceStatus.PRESENT)
            .OrderByDescending(a => a.CheckInUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (attendance is null)
        {
            return Denied("CHILD_NOT_PRESENT", "Child does not currently have an active attendance session.");
        }

        var camera = await _db.Cameras
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == cameraId, cancellationToken);

        if (camera is null)
        {
            return Denied("CAMERA_NOT_FOUND", "Camera not found.");
        }

        var cameraAssignedToRoom = await _db.CameraRooms
            .AsNoTracking()
            .AnyAsync(cr => cr.CameraId == cameraId
                            && cr.RoomId == child.RoomId
                            && (cr.ValidToUtc == null || cr.ValidToUtc > now),
                cancellationToken);

        if (!cameraAssignedToRoom)
        {
            return Denied("CAMERA_ACCESS_DENIED", "Camera is not assigned to this child's room.");
        }

        if (!camera.IsActive || camera.Status != CameraStatus.ACTIVE)
        {
            return Denied("CAMERA_NOT_AVAILABLE", "Camera is not currently available.");
        }

        return new CameraAccessDecision(true, null, null, attendance.Id, parent.Id);
    }

    private static CameraAccessDecision Denied(string code, string message) => new(false, code, message);
}
