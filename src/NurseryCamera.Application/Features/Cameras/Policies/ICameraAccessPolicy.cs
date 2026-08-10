namespace NurseryCamera.Application.Features.Cameras.Policies;

/// <summary>
/// Central, server-side object-level authorization boundary for parent camera viewing
/// (spec section 8/18/47): Parent -> ParentChildren -> Child -> Attendance -> Room -> Camera.
/// Must fail closed: any missing/invalid step in the chain results in <c>Allowed = false</c>.
/// </summary>
public interface ICameraAccessPolicy
{
    Task<CameraAccessDecision> CanViewAsync(Guid userId, Guid childId, Guid cameraId, CancellationToken cancellationToken);
}

public sealed record CameraAccessDecision(
    bool Allowed,
    string? DenialCode,
    string? DenialMessage,
    Guid? AttendanceSessionId = null,
    Guid? ParentId = null);
