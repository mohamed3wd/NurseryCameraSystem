using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NurseryCamera.Application.Abstractions.Identity;
using NurseryCamera.Application.Abstractions.Security;
using NurseryCamera.Application.Abstractions.Time;
using NurseryCamera.Application.Common.Exceptions;
using NurseryCamera.Application.Common.Options;
using NurseryCamera.Application.Features.Auth.Dtos;

namespace NurseryCamera.Infrastructure.Identity;

/// <summary>
/// Issues short-lived JWT access tokens and rotates refresh tokens on top of
/// ASP.NET Core Identity (spec sections 23/35). Refresh tokens are stored only as
/// a hash on the user record (<see cref="ApplicationUser.RefreshToken"/>); the raw
/// value is returned to the caller exactly once.
/// </summary>
public sealed class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenHashService _tokenHashService;
    private readonly IClock _clock;
    private readonly JwtOptions _jwtOptions;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        ITokenHashService tokenHashService,
        IClock clock,
        IOptions<JwtOptions> jwtOptions)
    {
        _userManager = userManager;
        _tokenHashService = tokenHashService;
        _clock = clock;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<AuthResponse> LoginAsync(string email, string password, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null || !user.IsActive)
        {
            throw AppException.InvalidCredentials();
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            // Deliberately the same message/code as bad credentials so account
            // lockout state cannot be enumerated by an attacker.
            throw AppException.InvalidCredentials();
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, password);
        if (!passwordValid)
        {
            await _userManager.AccessFailedAsync(user);
            throw AppException.InvalidCredentials();
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var providedHash = _tokenHashService.Hash(refreshToken);

        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.RefreshToken == providedHash, cancellationToken);

        if (user is null || !user.IsActive)
        {
            throw InvalidRefreshToken();
        }

        if (user.RefreshTokenExpiresAtUtc is null || user.RefreshTokenExpiresAtUtc <= _clock.UtcNow)
        {
            throw InvalidRefreshToken();
        }

        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task LogoutAsync(Guid userId, string? refreshToken, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return;
        }

        if (refreshToken is not null &&
            user.RefreshToken != _tokenHashService.Hash(refreshToken))
        {
            // Caller presented a refresh token that doesn't belong to the current
            // session; do not clear a different, still-valid session's token.
            return;
        }

        user.RefreshToken = null;
        user.RefreshTokenExpiresAtUtc = null;
        user.UpdatedAtUtc = _clock.UtcNow;
        await _userManager.UpdateAsync(user);
    }

    public async Task<UserDto> GetMeAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            throw AppException.NotFound("USER_NOT_FOUND", "User not found.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        return ToUserDto(user, roles);
    }

    private async Task<AuthResponse> IssueTokensAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var roles = await _userManager.GetRolesAsync(user);

        var accessTokenExpiresAtUtc = _clock.UtcNow.AddMinutes(_jwtOptions.AccessTokenMinutes);
        var accessToken = GenerateAccessToken(user, roles, accessTokenExpiresAtUtc);

        var rawRefreshToken = GenerateRawRefreshToken();
        user.RefreshToken = _tokenHashService.Hash(rawRefreshToken);
        user.RefreshTokenExpiresAtUtc = _clock.UtcNow.AddDays(_jwtOptions.RefreshTokenDays);
        user.UpdatedAtUtc = _clock.UtcNow;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            throw new AppException(
                "TOKEN_ISSUANCE_FAILED",
                "Failed to persist refresh token.",
                500);
        }

        return new AuthResponse(accessToken, rawRefreshToken, accessTokenExpiresAtUtc, ToUserDto(user, roles));
    }

    private static UserDto ToUserDto(ApplicationUser user, IList<string> roles) => new(
        user.Id,
        user.Email ?? string.Empty,
        user.FullName,
        user.PhoneNumber,
        user.IsActive,
        roles.ToArray());

    private string GenerateAccessToken(ApplicationUser user, IList<string> roles, DateTime expiresAtUtc)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, user.FullName)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var signingKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_jwtOptions.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            notBefore: _clock.UtcNow,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRawRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    private static AppException InvalidRefreshToken()
        => new("INVALID_REFRESH_TOKEN", "Invalid or expired refresh token.", 401);
}
