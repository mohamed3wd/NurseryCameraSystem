namespace NurseryCamera.Domain.Events;

public sealed record StreamTokenRevoked(
    Guid StreamTokenId,
    Guid ViewingSessionId,
    DateTime RevokedAtUtc);
