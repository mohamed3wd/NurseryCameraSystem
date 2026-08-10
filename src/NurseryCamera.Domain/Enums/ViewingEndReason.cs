namespace NurseryCamera.Domain.Enums;

public enum ViewingEndReason
{
    PARENT_STOPPED = 0,
    CHILD_CHECKED_OUT = 1,
    TOKEN_EXPIRED = 2,
    SESSION_EXPIRED = 3,
    CAMERA_OFFLINE = 4,
    ADMIN_REVOKED = 5,
    SECURITY_POLICY = 6,
    SYSTEM_ERROR = 7
}
