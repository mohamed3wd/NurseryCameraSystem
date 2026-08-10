namespace NurseryCamera.Application.Abstractions.Security;

/// <summary>
/// Hashes stream tokens (and device identifiers) before persistence. Only the hash is ever
/// stored (spec section 14) - the raw value is returned to the caller exactly once.
/// </summary>
public interface ITokenHashService
{
    /// <summary>Computes a SHA-256 hash (hex-encoded) of <paramref name="rawValue"/>.</summary>
    string Hash(string rawValue);

    /// <summary>Constant-time comparison of a raw value against a previously stored hash.</summary>
    bool Verify(string rawValue, string hash);
}
