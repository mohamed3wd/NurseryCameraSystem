using System.Security.Cryptography;
using NurseryCamera.Application.Abstractions.Security;

namespace NurseryCamera.Infrastructure.Security;

/// <summary>
/// Generates cryptographically random, base64url-encoded stream tokens
/// (spec section 14: never cameraId+timestamp, never predictable IDs).
/// </summary>
public sealed class StreamTokenGenerator : IStreamTokenGenerator
{
    private const int TokenSizeBytes = 32;

    public string Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(TokenSizeBytes);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}
