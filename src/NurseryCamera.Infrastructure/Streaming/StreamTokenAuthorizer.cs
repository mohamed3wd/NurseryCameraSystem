using Microsoft.EntityFrameworkCore;
using NurseryCamera.Application.Abstractions.Persistence;
using NurseryCamera.Application.Abstractions.Streaming;
using NurseryCamera.Domain.Enums;

namespace NurseryCamera.Infrastructure.Streaming;

/// <summary>
/// The single implementation of the stream-token validation chain from spec section 14:
/// token exists and its hash matches, is neither revoked nor expired, the viewing session is
/// ACTIVE, the child is still PRESENT, and the camera is still available.
///
/// Both live stream service implementations and the media-gateway source resolver share this so
/// the rules can never drift apart. The facts are read as a flat projection instead of an
/// <c>Include</c> graph: this runs on the stream start path, and only six scalar columns out of
/// three entities are actually needed - notably not the camera's encrypted credentials.
/// </summary>
internal static class StreamTokenAuthorizer
{
    internal sealed record Facts(
        StreamTokenStatus TokenStatus,
        DateTime TokenExpiresAtUtc,
        ViewingSessionStatus SessionStatus,
        AttendanceStatus AttendanceStatus,
        bool CameraIsActive,
        CameraStatus CameraStatus);

    public static Task<Facts?> LoadFactsAsync(
        IAppDbContext dbContext,
        Guid viewingSessionId,
        string tokenHash,
        CancellationToken cancellationToken) =>
        dbContext.StreamTokens
            .AsNoTracking()
            .Where(t => t.ViewingSessionId == viewingSessionId && t.TokenHash == tokenHash)
            .Select(t => new Facts(
                t.Status,
                t.ExpiresAtUtc,
                t.ViewingSession.Status,
                t.ViewingSession.AttendanceSession.Status,
                t.ViewingSession.Camera.IsActive,
                t.ViewingSession.Camera.Status))
            .FirstOrDefaultAsync(cancellationToken);

    public static StreamAuthorizationResult Evaluate(Facts? facts, DateTime utcNow)
    {
        if (facts is null)
        {
            return Deny("STREAM_TOKEN_NOT_FOUND", "Stream token is invalid.");
        }

        if (facts.TokenStatus == StreamTokenStatus.REVOKED)
        {
            return Deny("VIEWING_SESSION_REVOKED", "Stream token has been revoked.");
        }

        if (facts.TokenStatus != StreamTokenStatus.ACTIVE || facts.TokenExpiresAtUtc <= utcNow)
        {
            return Deny("VIEWING_SESSION_EXPIRED", "Stream token has expired.");
        }

        if (facts.SessionStatus != ViewingSessionStatus.ACTIVE)
        {
            return Deny("VIEWING_SESSION_NOT_FOUND", "Viewing session is not active.");
        }

        if (facts.AttendanceStatus != AttendanceStatus.PRESENT)
        {
            return Deny("CHILD_NOT_PRESENT", "Child is not currently present.");
        }

        if (!facts.CameraIsActive || facts.CameraStatus != CameraStatus.ACTIVE)
        {
            return Deny("CAMERA_NOT_AVAILABLE", "Camera is not available.");
        }

        return new StreamAuthorizationResult(Authorized: true);
    }

    private static StreamAuthorizationResult Deny(string code, string message)
        => new(Authorized: false, DenialCode: code, DenialMessage: message);
}
