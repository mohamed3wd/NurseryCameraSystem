namespace NurseryCamera.Api.Contracts;

public sealed record CreateCameraRequest(
    Guid NurseryId,
    string Name,
    string? Location,
    string RtspUrl,
    string? Username,
    string? Password,
    string? StreamProfile);

public sealed record UpdateCameraRequest(
    string Name,
    string? Location,
    string? RtspUrl,
    string? Username,
    string? Password,
    string? StreamProfile);
