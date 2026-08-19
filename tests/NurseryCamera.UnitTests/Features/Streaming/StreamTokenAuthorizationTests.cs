using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NurseryCamera.Application.Abstractions.Security;
using NurseryCamera.Application.Abstractions.Streaming;
using NurseryCamera.Application.Abstractions.Time;
using NurseryCamera.Domain.Entities;
using NurseryCamera.Domain.Enums;
using NurseryCamera.Infrastructure.Security;
using NurseryCamera.Infrastructure.Streaming;
using NurseryCamera.UnitTests.Helpers;

namespace NurseryCamera.UnitTests.Features.Streaming;

public sealed class StreamTokenAuthorizationTests
{
    private readonly DateTime _utcNow = new(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc);
    private readonly TokenHashService _hashService = new();

    [Fact]
    public async Task AuthorizeAsync_Allows_ValidActiveTokenChain()
    {
        var (service, rawToken, sessionId) = await CreateAuthorizedStreamAsync();

        var result = await service.AuthorizeAsync(
            new StreamAuthorizationRequest(sessionId, rawToken),
            CancellationToken.None);

        result.Authorized.Should().BeTrue();
    }

    [Fact]
    public async Task AuthorizeAsync_Denies_ExpiredToken()
    {
        var (service, rawToken, sessionId, db, token) = await CreateAuthorizedStreamFullAsync();
        token.ExpiresAtUtc = _utcNow.AddMinutes(-1);
        await db.SaveChangesAsync();

        var result = await service.AuthorizeAsync(
            new StreamAuthorizationRequest(sessionId, rawToken),
            CancellationToken.None);

        result.Authorized.Should().BeFalse();
        result.DenialCode.Should().Be("VIEWING_SESSION_EXPIRED");
    }

    [Fact]
    public async Task AuthorizeAsync_Denies_RevokedToken()
    {
        var (service, rawToken, sessionId, db, token) = await CreateAuthorizedStreamFullAsync();
        token.Status = StreamTokenStatus.REVOKED;
        token.RevokedAtUtc = _utcNow;
        await db.SaveChangesAsync();

        var result = await service.AuthorizeAsync(
            new StreamAuthorizationRequest(sessionId, rawToken),
            CancellationToken.None);

        result.Authorized.Should().BeFalse();
        result.DenialCode.Should().Be("VIEWING_SESSION_REVOKED");
    }

    [Fact]
    public async Task AuthorizeAsync_Denies_WrongTokenForSession()
    {
        var (service, _, sessionId) = await CreateAuthorizedStreamAsync();

        var result = await service.AuthorizeAsync(
            new StreamAuthorizationRequest(sessionId, "totally-wrong-token"),
            CancellationToken.None);

        result.Authorized.Should().BeFalse();
        result.DenialCode.Should().Be("STREAM_TOKEN_NOT_FOUND");
    }

    [Fact]
    public async Task AuthorizeAsync_Denies_WhenAttendanceNoLongerPresent()
    {
        var (service, rawToken, sessionId, db, _) = await CreateAuthorizedStreamFullAsync();
        var attendance = db.AttendanceSessions.Single();
        attendance.Status = AttendanceStatus.COMPLETED;
        attendance.CheckOutUtc = _utcNow;
        await db.SaveChangesAsync();

        var result = await service.AuthorizeAsync(
            new StreamAuthorizationRequest(sessionId, rawToken),
            CancellationToken.None);

        result.Authorized.Should().BeFalse();
        result.DenialCode.Should().Be("CHILD_NOT_PRESENT");
    }

    [Fact]
    public async Task AuthorizeAsync_Denies_ForeignTokenReplay()
    {
        var (service, _, sessionId, db, _) = await CreateAuthorizedStreamFullAsync();

        var result = await service.AuthorizeAsync(
            new StreamAuthorizationRequest(sessionId, "other-parent-token-value"),
            CancellationToken.None);

        result.Authorized.Should().BeFalse();
        db.ViewingSessions.Should().ContainSingle(v => v.Id == sessionId);
    }

    private async Task<(MockLiveStreamService Service, string RawToken, Guid SessionId)> CreateAuthorizedStreamAsync()
    {
        var full = await CreateAuthorizedStreamFullAsync();
        return (full.Service, full.RawToken, full.SessionId);
    }

    private async Task<(MockLiveStreamService Service, string RawToken, Guid SessionId, Infrastructure.Persistence.AppDbContext Db, StreamToken Token)>
        CreateAuthorizedStreamFullAsync()
    {
        var db = InMemoryDbFactory.Create();
        var seed = await CameraAccessSeed.CreateAuthorizedAsync(db, _utcNow);

        var sessionId = Guid.NewGuid();
        var rawToken = "raw-stream-token-abc123";

        db.ViewingSessions.Add(new ViewingSession
        {
            Id = sessionId,
            ParentId = seed.ParentId,
            ChildId = seed.ChildId,
            CameraId = seed.CameraId,
            AttendanceSessionId = seed.AttendanceSessionId,
            StartedAtUtc = _utcNow,
            ExpiresAtUtc = _utcNow.AddMinutes(15),
            Status = ViewingSessionStatus.ACTIVE,
            ClientType = "web"
        });

        var token = new StreamToken
        {
            Id = Guid.NewGuid(),
            ViewingSessionId = sessionId,
            TokenHash = _hashService.Hash(rawToken),
            IssuedAtUtc = _utcNow,
            ExpiresAtUtc = _utcNow.AddMinutes(1),
            Status = StreamTokenStatus.ACTIVE
        };
        db.StreamTokens.Add(token);
        await db.SaveChangesAsync();

        var encryption = new Mock<ISecretEncryptionService>();
        encryption.Setup(e => e.Decrypt(It.IsAny<string>())).Returns("rtsp://192.0.2.10/demo");

        var clock = new Mock<IClock>();
        clock.Setup(c => c.UtcNow).Returns(_utcNow);

        var service = new MockLiveStreamService(
            db,
            encryption.Object,
            _hashService,
            clock.Object,
            NullLogger<MockLiveStreamService>.Instance);

        return (service, rawToken, sessionId, db, token);
    }
}
