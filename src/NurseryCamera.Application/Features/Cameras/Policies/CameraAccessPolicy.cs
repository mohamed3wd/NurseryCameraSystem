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

        // Everything the parent/child/attendance half of the algorithm needs, projected into a
        // single round trip. This runs on the critical path of every start-view request, and the
        // step-by-step version issued four sequential queries before it could reach step 5.
        var context = await _db.Parents
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => new
            {
                ParentId = p.Id,
                ParentStatus = p.Status,
                Relation = _db.ParentChildren
                    .Where(pc => pc.ParentId == p.Id && pc.ChildId == childId)
                    .Select(pc => new { pc.CanViewCamera })
                    .FirstOrDefault(),
                Child = _db.Children
                    .Where(c => c.Id == childId)
                    .Select(c => new { c.IsActive, c.RoomId })
                    .FirstOrDefault(),
                AttendanceSessionId = _db.AttendanceSessions
                    .Where(a => a.ChildId == childId && a.Status == AttendanceStatus.PRESENT)
                    .OrderByDescending(a => a.CheckInUtc)
                    .Select(a => (Guid?)a.Id)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (context is null || context.ParentStatus != ParentStatus.Active)
        {
            return Denied("FORBIDDEN", "No active parent profile was found for this account.");
        }

        if (context.Relation is null)
        {
            return Denied("PARENT_CHILD_RELATION_NOT_FOUND", "This child is not linked to your account.");
        }

        if (!context.Relation.CanViewCamera)
        {
            return Denied("CAMERA_ACCESS_DENIED", "Camera viewing has not been enabled for this child.");
        }

        if (context.Child is null || !context.Child.IsActive)
        {
            return Denied("CHILD_NOT_FOUND", "Child not found.");
        }

        if (context.Child.RoomId is not { } childRoomId)
        {
            return Denied("CAMERA_ACCESS_DENIED", "Child is not currently assigned to a room.");
        }

        if (context.AttendanceSessionId is not { } attendanceSessionId)
        {
            return Denied("CHILD_NOT_PRESENT", "Child does not currently have an active attendance session.");
        }

        var camera = await _db.Cameras
            .AsNoTracking()
            .Where(c => c.Id == cameraId)
            .Select(c => new
            {
                c.IsActive,
                c.Status,
                AssignedToChildRoom = _db.CameraRooms.Any(cr => cr.CameraId == cameraId
                                                                && cr.RoomId == childRoomId
                                                                && (cr.ValidToUtc == null || cr.ValidToUtc > now))
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (camera is null)
        {
            return Denied("CAMERA_NOT_FOUND", "Camera not found.");
        }

        if (!camera.AssignedToChildRoom)
        {
            return Denied("CAMERA_ACCESS_DENIED", "Camera is not assigned to this child's room.");
        }

        if (!camera.IsActive || camera.Status != CameraStatus.ACTIVE)
        {
            return Denied("CAMERA_NOT_AVAILABLE", "Camera is not currently available.");
        }

        return new CameraAccessDecision(true, null, null, attendanceSessionId, context.ParentId);
    }

    private static CameraAccessDecision Denied(string code, string message) => new(false, code, message);
}
