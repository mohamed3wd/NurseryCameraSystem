using NurseryCamera.Application.Features.Auth.Dtos;

namespace NurseryCamera.Application.Abstractions.Identity;

/// <summary>
/// Wraps ASP.NET Core Identity (implemented in Infrastructure) so the Application layer
/// never needs to reference <c>ApplicationUser</c>/<c>UserManager</c> directly.
/// </summary>
public interface IAuthService
{
    /// <summary>Throws <see cref="Common.Exceptions.AppException"/> (INVALID_CREDENTIALS) on failure.</summary>
    Task<AuthResponse> LoginAsync(string email, string password, CancellationToken cancellationToken);

    /// <summary>Rotates the refresh token. Throws on an invalid/expired/revoked token.</summary>
    Task<AuthResponse> RefreshAsync(string refreshToken, CancellationToken cancellationToken);

    Task LogoutAsync(Guid userId, string? refreshToken, CancellationToken cancellationToken);

    Task<UserDto> GetMeAsync(Guid userId, CancellationToken cancellationToken);
}
