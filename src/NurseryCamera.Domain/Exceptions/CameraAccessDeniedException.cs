namespace NurseryCamera.Domain.Exceptions;

public sealed class CameraAccessDeniedException : DomainException
{
    public CameraAccessDeniedException(string message = "Camera access was denied.")
        : base("CAMERA_ACCESS_DENIED", message)
    {
    }
}
