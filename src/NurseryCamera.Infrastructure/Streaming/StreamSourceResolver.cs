using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NurseryCamera.Application.Abstractions.Persistence;
using NurseryCamera.Application.Abstractions.Security;
using NurseryCamera.Application.Abstractions.Streaming;
using NurseryCamera.Application.Common.Options;

namespace NurseryCamera.Infrastructure.Streaming;

/// <summary>
/// Resolves an authorized viewing session into a private media source for the media gateway only.
/// Never expose results to parent/browser clients.
/// </summary>
public interface IStreamSourceResolver
{
    Task<StreamSourceResolveResult> ResolveAsync(
        Guid viewingSessionId,
        string rawStreamToken,
        CancellationToken cancellationToken);
}

public sealed record StreamSourceResolveResult(
    bool Authorized,
    string? StreamName,
    string? SourceUrl,
    string? DenialCode = null,
    string? DenialMessage = null);

public sealed class StreamSourceResolver : IStreamSourceResolver
{
    private readonly ILiveStreamService _liveStreamService;
    private readonly IAppDbContext _dbContext;
    private readonly ISecretEncryptionService _encryptionService;
    private readonly MediaGatewayOptions _options;

    public StreamSourceResolver(
        ILiveStreamService liveStreamService,
        IAppDbContext dbContext,
        ISecretEncryptionService encryptionService,
        IOptions<MediaGatewayOptions> options)
    {
        _liveStreamService = liveStreamService;
        _dbContext = dbContext;
        _encryptionService = encryptionService;
        _options = options.Value;
    }

    public async Task<StreamSourceResolveResult> ResolveAsync(
        Guid viewingSessionId,
        string rawStreamToken,
        CancellationToken cancellationToken)
    {
        var auth = await _liveStreamService.AuthorizeAsync(
            new StreamAuthorizationRequest(viewingSessionId, rawStreamToken),
            cancellationToken);

        if (!auth.Authorized)
        {
            return new StreamSourceResolveResult(
                false, null, null, auth.DenialCode, auth.DenialMessage);
        }

        // Only the three encrypted columns are read back; loading the ViewingSession and Camera
        // entities in full would pull far more data than the gateway hand-off needs.
        var secrets = await _dbContext.ViewingSessions
            .AsNoTracking()
            .Where(v => v.Id == viewingSessionId)
            .Select(v => new
            {
                v.Camera.RtspUrlEncrypted,
                v.Camera.UsernameEncrypted,
                v.Camera.PasswordEncrypted
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (secrets is null)
        {
            return new StreamSourceResolveResult(
                false, null, null, "VIEWING_SESSION_NOT_FOUND", "Viewing session was not found.");
        }

        var streamName = $"vs_{viewingSessionId:N}";
        string source;
        try
        {
            source = BuildSource(
                secrets.RtspUrlEncrypted,
                secrets.UsernameEncrypted,
                secrets.PasswordEncrypted);
        }
        catch
        {
            source = _options.DemoSource;
        }

        return new StreamSourceResolveResult(true, streamName, source);
    }

    private string BuildSource(string rtspEncrypted, string usernameEncrypted, string passwordEncrypted)
    {
        var rtsp = _encryptionService.Decrypt(rtspEncrypted);
        if (string.IsNullOrWhiteSpace(rtsp) ||
            rtsp.StartsWith("demo://", StringComparison.OrdinalIgnoreCase) ||
            rtsp.Contains("192.0.2.", StringComparison.Ordinal))
        {
            return _options.DemoSource;
        }

        var username = _encryptionService.Decrypt(usernameEncrypted);
        var password = _encryptionService.Decrypt(passwordEncrypted);

        if (!string.IsNullOrWhiteSpace(username) &&
            rtsp.StartsWith("rtsp://", StringComparison.OrdinalIgnoreCase) &&
            !rtsp.Contains('@'))
        {
            var rest = rtsp["rtsp://".Length..];
            return $"rtsp://{Uri.EscapeDataString(username)}:{Uri.EscapeDataString(password)}@{rest}";
        }

        return rtsp;
    }
}
