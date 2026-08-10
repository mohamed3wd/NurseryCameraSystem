namespace NurseryCamera.Application.Abstractions.Identity;

/// <summary>
/// Resolves the identity of the caller for the current request, sourced from the
/// authenticated ASP.NET Identity <c>ApplicationUser</c> claims in the Infrastructure/API layer.
/// The Application layer only ever deals with the <see cref="Guid"/> UserId, never
/// with ASP.NET Identity types directly.
/// </summary>
public interface ICurrentUser
{
    Guid? UserId { get; }
    bool IsAuthenticated { get; }
}
