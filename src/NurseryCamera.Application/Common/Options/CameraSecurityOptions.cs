namespace NurseryCamera.Application.Common.Options;

/// <summary>
/// Bound from the "CameraSecurity" configuration section. See spec section 35.
/// Camera RTSP URLs/credentials are encrypted at rest using a key resolved from this reference
/// (e.g. environment variable, KeyVault secret name) - never stored in plain configuration.
/// </summary>
public sealed class CameraSecurityOptions
{
    public const string SectionName = "CameraSecurity";

    public string EncryptionKeyReference { get; set; } = string.Empty;
}
