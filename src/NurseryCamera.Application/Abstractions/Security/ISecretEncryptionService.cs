namespace NurseryCamera.Application.Abstractions.Security;

/// <summary>
/// Encrypts/decrypts camera secrets (RTSP URL, username, password) at rest (BR-015).
/// Implementations must never log plaintext or ciphertext values (BR-016).
/// </summary>
public interface ISecretEncryptionService
{
    /// <summary>Encrypts <paramref name="plaintext"/>. Never log the input or output.</summary>
    string Encrypt(string plaintext);

    /// <summary>Decrypts <paramref name="ciphertext"/>. Never log the input or output.</summary>
    string Decrypt(string ciphertext);
}
