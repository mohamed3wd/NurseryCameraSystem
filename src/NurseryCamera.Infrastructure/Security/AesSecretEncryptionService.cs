using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NurseryCamera.Application.Abstractions.Security;
using NurseryCamera.Application.Common.Options;

namespace NurseryCamera.Infrastructure.Security;

/// <summary>
/// AES-256-GCM encryption for camera secrets at rest (BR-015). The key comes from
/// CameraSecurity:EncryptionKey (base64, 32 bytes). Never logs plaintext values.
/// </summary>
public sealed class AesSecretEncryptionService : ISecretEncryptionService
{
    private const int NonceSizeBytes = 12;
    private const int TagSizeBytes = 16;

    private readonly byte[] _key;
    private readonly ILogger<AesSecretEncryptionService> _logger;

    public AesSecretEncryptionService(
        IOptions<CameraSecurityOptions> options,
        ILogger<AesSecretEncryptionService> logger)
    {
        _logger = logger;

        // NOTE: The Application-layer CameraSecurityOptions only exposes EncryptionKeyReference
        // (a reference/secret name resolved by the host from environment/secret manager - see
        // spec section 35/23). This MVP has no secret manager integration wired up yet, so the
        // reference is treated as containing the resolved base64-encoded 32-byte AES key directly
        // (e.g. bound from the CameraSecurity:EncryptionKeyReference configuration/env value).
        var keyBase64 = options.Value.EncryptionKeyReference;
        if (string.IsNullOrWhiteSpace(keyBase64))
        {
            throw new InvalidOperationException(
                "CameraSecurity:EncryptionKeyReference is not configured. A base64-encoded 32-byte AES key is required.");
        }

        byte[] key;
        try
        {
            key = Convert.FromBase64String(keyBase64);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException("CameraSecurity:EncryptionKeyReference must be valid base64.", ex);
        }

        if (key.Length != 32)
        {
            throw new InvalidOperationException(
                $"CameraSecurity:EncryptionKeyReference must decode to 32 bytes (256-bit AES key). Got {key.Length} bytes.");
        }

        _key = key;
    }

    public string Encrypt(string plaintext)
    {
        ArgumentNullException.ThrowIfNull(plaintext);

        var plaintextBytes = System.Text.Encoding.UTF8.GetBytes(plaintext);
        var nonce = RandomNumberGenerator.GetBytes(NonceSizeBytes);
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[TagSizeBytes];

        using (var aesGcm = new AesGcm(_key, TagSizeBytes))
        {
            aesGcm.Encrypt(nonce, plaintextBytes, ciphertext, tag);
        }

        // Layout: nonce || tag || ciphertext, base64-encoded.
        var combined = new byte[nonce.Length + tag.Length + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, combined, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, combined, nonce.Length, tag.Length);
        Buffer.BlockCopy(ciphertext, 0, combined, nonce.Length + tag.Length, ciphertext.Length);

        return Convert.ToBase64String(combined);
    }

    public string Decrypt(string ciphertext)
    {
        ArgumentNullException.ThrowIfNull(ciphertext);

        byte[] combined;
        try
        {
            combined = Convert.FromBase64String(ciphertext);
        }
        catch (FormatException ex)
        {
            _logger.LogError(ex, "Failed to decrypt secret: payload is not valid base64.");
            throw new CryptographicException("Invalid encrypted payload.", ex);
        }

        if (combined.Length < NonceSizeBytes + TagSizeBytes)
        {
            _logger.LogError("Failed to decrypt secret: payload too short.");
            throw new CryptographicException("Invalid encrypted payload.");
        }

        var nonce = combined.AsSpan(0, NonceSizeBytes).ToArray();
        var tag = combined.AsSpan(NonceSizeBytes, TagSizeBytes).ToArray();
        var cipherBytes = combined.AsSpan(NonceSizeBytes + TagSizeBytes).ToArray();
        var plaintextBytes = new byte[cipherBytes.Length];

        using (var aesGcm = new AesGcm(_key, TagSizeBytes))
        {
            aesGcm.Decrypt(nonce, cipherBytes, tag, plaintextBytes);
        }

        return System.Text.Encoding.UTF8.GetString(plaintextBytes);
    }
}
