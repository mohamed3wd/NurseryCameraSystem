namespace NurseryCamera.Api.Contracts;

public sealed record CreateNurseryRequest(
    string Name,
    string TimeZoneId,
    string? Address);
