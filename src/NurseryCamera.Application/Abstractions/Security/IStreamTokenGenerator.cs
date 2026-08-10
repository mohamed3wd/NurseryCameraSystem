namespace NurseryCamera.Application.Abstractions.Security;

/// <summary>
/// Generates cryptographically secure, unpredictable stream authorization tokens
/// (spec section 14). Never derived from camera id + timestamp or any other predictable input.
/// </summary>
public interface IStreamTokenGenerator
{
    /// <summary>Returns a new cryptographically random, URL-safe token string.</summary>
    string Generate();
}
