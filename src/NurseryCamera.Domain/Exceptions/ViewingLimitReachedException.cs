namespace NurseryCamera.Domain.Exceptions;

public sealed class ViewingLimitReachedException : DomainException
{
    public ViewingLimitReachedException(string message = "Viewing session limit has been reached.")
        : base("VIEWING_LIMIT_REACHED", message)
    {
    }
}
