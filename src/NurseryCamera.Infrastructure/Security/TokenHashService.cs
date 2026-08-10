using System.Security.Cryptography;
using System.Text;
using NurseryCamera.Application.Abstractions.Security;

namespace NurseryCamera.Infrastructure.Security;

/// <summary>
/// SHA-256 one-way hash used to store stream/refresh tokens without ever
/// persisting the raw secret (spec section 14).
/// </summary>
public sealed class TokenHashService : ITokenHashService
{
    public string Hash(string rawValue)
    {
        ArgumentException.ThrowIfNullOrEmpty(rawValue);

        var bytes = Encoding.UTF8.GetBytes(rawValue);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public bool Verify(string rawValue, string hash)
    {
        ArgumentException.ThrowIfNullOrEmpty(rawValue);
        ArgumentException.ThrowIfNullOrEmpty(hash);

        var computed = Hash(rawValue);
        var computedBytes = Encoding.UTF8.GetBytes(computed);
        var expectedBytes = Encoding.UTF8.GetBytes(hash);

        return computedBytes.Length == expectedBytes.Length &&
               CryptographicOperations.FixedTimeEquals(computedBytes, expectedBytes);
    }
}
