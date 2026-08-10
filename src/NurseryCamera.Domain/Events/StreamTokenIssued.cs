namespace NurseryCamera.Domain.Events;

public sealed record StreamTokenIssued(
    Guid StreamTokenId,
    Guid ViewingSessionId,
    DateTime IssuedAtUtc,
    DateTime ExpiresAtUtc);
