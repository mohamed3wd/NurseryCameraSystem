namespace NurseryCamera.Api.Contracts;

public sealed record StartViewingSessionRequest(string ClientType, string? DeviceId);
