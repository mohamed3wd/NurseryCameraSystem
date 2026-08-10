using NurseryCamera.Domain.Enums;

namespace NurseryCamera.Domain.Events;

public sealed record ViewingSessionEnded(
    Guid ViewingSessionId,
    Guid ParentId,
    Guid ChildId,
    Guid CameraId,
    ViewingEndReason EndReason,
    DateTime EndedAtUtc);
