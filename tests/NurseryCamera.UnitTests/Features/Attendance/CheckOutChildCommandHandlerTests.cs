using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NurseryCamera.Application.Abstractions.Audit;
using NurseryCamera.Application.Abstractions.Identity;
using NurseryCamera.Application.Abstractions.Notifications;
using NurseryCamera.Application.Abstractions.Streaming;
using NurseryCamera.Application.Abstractions.Time;
using NurseryCamera.Application.Behaviors;
using NurseryCamera.Application.Features.Attendance.Commands;
using NurseryCamera.Application.Features.Attendance.Dtos;
using NurseryCamera.Domain.Entities;
using NurseryCamera.Domain.Enums;
using NurseryCamera.UnitTests.Helpers;

namespace NurseryCamera.UnitTests.Features.Attendance;

public sealed class CheckOutChildCommandHandlerTests
{
    private readonly DateTime _utcNow = new(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Handle_RevokesActiveViewingSessions_AndStreamTokens()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using var db = InMemoryDbFactory.Create(databaseName);
        var seed = await CameraAccessSeed.CreateAuthorizedAsync(db, _utcNow);

        var activeSessionId = Guid.NewGuid();
        var pendingSessionId = Guid.NewGuid();
        var endedSessionId = Guid.NewGuid();
        var activeTokenId = Guid.NewGuid();
        var pendingTokenId = Guid.NewGuid();
        var alreadyRevokedTokenId = Guid.NewGuid();

        db.ViewingSessions.AddRange(
            new ViewingSession
            {
                Id = activeSessionId,
                ParentId = seed.ParentId,
                ChildId = seed.ChildId,
                CameraId = seed.CameraId,
                AttendanceSessionId = seed.AttendanceSessionId,
                StartedAtUtc = _utcNow.AddMinutes(-30),
                ExpiresAtUtc = _utcNow.AddMinutes(30),
                Status = ViewingSessionStatus.ACTIVE,
                ClientType = "ios"
            },
            new ViewingSession
            {
                Id = pendingSessionId,
                ParentId = seed.ParentId,
                ChildId = seed.ChildId,
                CameraId = seed.CameraId,
                AttendanceSessionId = seed.AttendanceSessionId,
                StartedAtUtc = _utcNow.AddMinutes(-5),
                ExpiresAtUtc = _utcNow.AddMinutes(55),
                Status = ViewingSessionStatus.PENDING,
                ClientType = "web"
            },
            new ViewingSession
            {
                Id = endedSessionId,
                ParentId = seed.ParentId,
                ChildId = seed.ChildId,
                CameraId = seed.CameraId,
                AttendanceSessionId = seed.AttendanceSessionId,
                StartedAtUtc = _utcNow.AddHours(-3),
                ExpiresAtUtc = _utcNow.AddHours(-2),
                EndedAtUtc = _utcNow.AddHours(-2),
                Status = ViewingSessionStatus.ENDED,
                EndReason = ViewingEndReason.PARENT_STOPPED,
                ClientType = "ios"
            });

        db.StreamTokens.AddRange(
            new StreamToken
            {
                Id = activeTokenId,
                ViewingSessionId = activeSessionId,
                TokenHash = "hash-active",
                IssuedAtUtc = _utcNow.AddMinutes(-30),
                ExpiresAtUtc = _utcNow.AddMinutes(30),
                Status = StreamTokenStatus.ACTIVE
            },
            new StreamToken
            {
                Id = pendingTokenId,
                ViewingSessionId = pendingSessionId,
                TokenHash = "hash-pending",
                IssuedAtUtc = _utcNow.AddMinutes(-5),
                ExpiresAtUtc = _utcNow.AddMinutes(55),
                Status = StreamTokenStatus.ACTIVE
            },
            new StreamToken
            {
                Id = alreadyRevokedTokenId,
                ViewingSessionId = endedSessionId,
                TokenHash = "hash-old",
                IssuedAtUtc = _utcNow.AddHours(-3),
                ExpiresAtUtc = _utcNow.AddHours(-2),
                RevokedAtUtc = _utcNow.AddHours(-2),
                Status = StreamTokenStatus.REVOKED
            });

        await db.SaveChangesAsync();

        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(u => u.UserId).Returns(seed.StaffUserId);

        var clock = new Mock<IClock>();
        clock.Setup(c => c.UtcNow).Returns(_utcNow);

        var audit = new Mock<IAuditService>();
        audit.Setup(a => a.LogAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var notifications = new Mock<INotificationService>();
        notifications
            .Setup(n => n.NotifyChildCheckedOutAsync(
                It.IsAny<Guid>(), It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        notifications
            .Setup(n => n.NotifyViewingSessionRevokedAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var liveStream = new Mock<ILiveStreamService>();
        liveStream
            .Setup(s => s.StopAsync(It.IsAny<StopStreamRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new CheckOutChildCommandHandler(
            db,
            currentUser.Object,
            clock.Object,
            audit.Object,
            notifications.Object,
            liveStream.Object,
            NullLogger<CheckOutChildCommandHandler>.Instance);

        // Run through the pipeline behavior, which is what actually commits in production now.
        var command = new CheckOutChildCommand(seed.ChildId);
        var result = await new UnitOfWorkBehavior<CheckOutChildCommand, AttendanceDto>(db)
            .Handle(command, ct => handler.Handle(command, ct), CancellationToken.None);

        result.Status.Should().Be(AttendanceStatus.COMPLETED.ToString());
        result.CheckOutUtc.Should().Be(_utcNow);

        // Read through a separate context so the assertions see committed rows rather than the
        // handler's still-tracked entity graph.
        await using var db2 = InMemoryDbFactory.Create(databaseName);

        var attendance = await db2.AttendanceSessions.SingleAsync(a => a.Id == seed.AttendanceSessionId);
        attendance.Status.Should().Be(AttendanceStatus.COMPLETED);
        attendance.CheckOutUtc.Should().Be(_utcNow);

        var active = await db2.ViewingSessions.SingleAsync(v => v.Id == activeSessionId);
        active.Status.Should().Be(ViewingSessionStatus.ENDED);
        active.EndedAtUtc.Should().Be(_utcNow);
        active.EndReason.Should().Be(ViewingEndReason.CHILD_CHECKED_OUT);

        var pending = await db2.ViewingSessions.SingleAsync(v => v.Id == pendingSessionId);
        pending.Status.Should().Be(ViewingSessionStatus.ENDED);
        pending.EndReason.Should().Be(ViewingEndReason.CHILD_CHECKED_OUT);

        var previouslyEnded = await db2.ViewingSessions.SingleAsync(v => v.Id == endedSessionId);
        previouslyEnded.Status.Should().Be(ViewingSessionStatus.ENDED);
        previouslyEnded.EndReason.Should().Be(ViewingEndReason.PARENT_STOPPED);

        var activeToken = await db2.StreamTokens.SingleAsync(t => t.Id == activeTokenId);
        activeToken.Status.Should().Be(StreamTokenStatus.REVOKED);
        activeToken.RevokedAtUtc.Should().Be(_utcNow);

        var pendingToken = await db2.StreamTokens.SingleAsync(t => t.Id == pendingTokenId);
        pendingToken.Status.Should().Be(StreamTokenStatus.REVOKED);
        pendingToken.RevokedAtUtc.Should().Be(_utcNow);

        liveStream.Verify(
            s => s.StopAsync(It.Is<StopStreamRequest>(r => r.ViewingSessionId == activeSessionId), It.IsAny<CancellationToken>()),
            Times.Once);
        liveStream.Verify(
            s => s.StopAsync(It.Is<StopStreamRequest>(r => r.ViewingSessionId == pendingSessionId), It.IsAny<CancellationToken>()),
            Times.Once);
        liveStream.Verify(
            s => s.StopAsync(It.Is<StopStreamRequest>(r => r.ViewingSessionId == endedSessionId), It.IsAny<CancellationToken>()),
            Times.Never);

        notifications.Verify(
            n => n.NotifyViewingSessionRevokedAsync(activeSessionId, seed.UserId, "CHILD_CHECKED_OUT", It.IsAny<CancellationToken>()),
            Times.Once);
        notifications.Verify(
            n => n.NotifyViewingSessionRevokedAsync(pendingSessionId, seed.UserId, "CHILD_CHECKED_OUT", It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
