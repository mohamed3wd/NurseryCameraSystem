namespace NurseryCamera.Domain.Exceptions;

public sealed class ChildNotPresentException : DomainException
{
    public ChildNotPresentException(string message = "Child is not currently present.")
        : base("CHILD_NOT_PRESENT", message)
    {
    }
}
