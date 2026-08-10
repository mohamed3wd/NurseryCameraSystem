using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using NurseryCamera.Application.Abstractions.Security;
using NurseryCamera.Application.Abstractions.Streaming;
using NurseryCamera.Application.Common.Options;
using NurseryCamera.Domain.Enums;
using NurseryCamera.Infrastructure.Streaming;
using NurseryCamera.UnitTests.Helpers;

namespace NurseryCamera.UnitTests.Features.Streaming;

public sealed class StreamSourceResolverTests
{
    private readonly DateTime _utcNow = new(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ResolveAsync_FailClosed_WhenAuthorizeDenies()
    {
        var db = InMemoryDbFactory.Create();
        await CameraAccessSeed.CreateAuthorizedAsync(db, _utcNow);

        var live = new Mock<ILiveStreamService>();
        live.Setup(s => s.AuthorizeAsync(It.IsAny<StreamAuthorizationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StreamAuthorizationResult(false, "STREAM_TOKEN_NOT_FOUND", "invalid"));

        var encryption = new Mock<ISecretEncryptionService>();
        var options = Options.Create(new MediaGatewayOptions());

        var resolver = new StreamSourceResolver(live.Object, db, encryption.Object, options);
        var result = await resolver.ResolveAsync(Guid.NewGuid(), "bad", CancellationToken.None);

        result.Authorized.Should().BeFalse();
        result.SourceUrl.Should().BeNull();
        result.DenialCode.Should().Be("STREAM_TOKEN_NOT_FOUND");
    }

    [Fact]
    public async Task ResolveAsync_MapsPlaceholderCameraToDemoSource()
    {
        var db = InMemoryDbFactory.Create();
        var seed = await CameraAccessSeed.CreateAuthorizedAsync(db, _utcNow);

        var sessionId = Guid.NewGuid();
        db.ViewingSessions.Add(new Domain.Entities.ViewingSession
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
        await db.SaveChangesAsync();

        var live = new Mock<ILiveStreamService>();
        live.Setup(s => s.AuthorizeAsync(It.IsAny<StreamAuthorizationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StreamAuthorizationResult(true));

        var encryption = new Mock<ISecretEncryptionService>();
        encryption.Setup(e => e.Decrypt("enc-rtsp")).Returns("rtsp://192.0.2.10:554/demo-stream");
        encryption.Setup(e => e.Decrypt("enc-user")).Returns("user");
        encryption.Setup(e => e.Decrypt("enc-pass")).Returns("pass");

        var options = Options.Create(new MediaGatewayOptions { DemoSource = "ffmpeg:testsrc" });
        var resolver = new StreamSourceResolver(live.Object, db, encryption.Object, options);

        var result = await resolver.ResolveAsync(sessionId, "token", CancellationToken.None);

        result.Authorized.Should().BeTrue();
        result.StreamName.Should().Be($"vs_{sessionId:N}");
        result.SourceUrl.Should().Be("ffmpeg:testsrc");
    }
}
