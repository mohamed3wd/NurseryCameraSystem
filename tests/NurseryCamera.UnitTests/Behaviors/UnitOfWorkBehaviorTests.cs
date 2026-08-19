using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NurseryCamera.Application.Behaviors;
using NurseryCamera.Application.Common.Exceptions;
using NurseryCamera.Domain.Entities;
using NurseryCamera.Domain.Exceptions;
using NurseryCamera.UnitTests.Helpers;

namespace NurseryCamera.UnitTests.Behaviors;

/// <summary>
/// Handlers, IAuditService and INotificationService no longer save anything themselves, so this
/// behavior is the only thing standing between a completed command and a lost write.
/// </summary>
public sealed class UnitOfWorkBehaviorTests
{
    [Fact]
    public async Task Handle_CommitsStagedChanges_WhenHandlerSucceeds()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using var db = InMemoryDbFactory.Create(databaseName);
        var behavior = new UnitOfWorkBehavior<string, string>(db);

        var result = await behavior.Handle(
            "request",
            _ =>
            {
                db.AuditLogs.Add(NewAuditLog("CAMERA_VIEW_AUTHORIZED", "SUCCESS"));
                return Task.FromResult("ok");
            },
            CancellationToken.None);

        result.Should().Be("ok");

        await using var verification = InMemoryDbFactory.Create(databaseName);
        (await verification.AuditLogs.CountAsync()).Should().Be(1);
    }

    [Theory]
    [MemberData(nameof(ExpectedBusinessFailures))]
    public async Task Handle_CommitsDenialTrail_WhenHandlerRejectsRequest(Exception expectedFailure)
    {
        var databaseName = Guid.NewGuid().ToString();
        await using var db = InMemoryDbFactory.Create(databaseName);
        var behavior = new UnitOfWorkBehavior<string, string>(db);

        var act = async () => await behavior.Handle(
            "request",
            _ =>
            {
                db.AuditLogs.Add(NewAuditLog("CAMERA_VIEW_DENIED", "DENIED"));
                throw expectedFailure;
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<Exception>();

        await using var verification = InMemoryDbFactory.Create(databaseName);
        var persisted = await verification.AuditLogs.SingleAsync();
        persisted.Result.Should().Be("DENIED");
    }

    [Fact]
    public async Task Handle_DiscardsStagedChanges_WhenHandlerFailsUnexpectedly()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using var db = InMemoryDbFactory.Create(databaseName);
        var behavior = new UnitOfWorkBehavior<string, string>(db);

        var act = async () => await behavior.Handle(
            "request",
            _ =>
            {
                db.AuditLogs.Add(NewAuditLog("CAMERA_VIEW_AUTHORIZED", "SUCCESS"));
                throw new InvalidOperationException("gateway blew up mid-handler");
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();

        await using var verification = InMemoryDbFactory.Create(databaseName);
        (await verification.AuditLogs.CountAsync()).Should().Be(0);
    }

    public static TheoryData<Exception> ExpectedBusinessFailures() => new()
    {
        AppException.Forbidden("CAMERA_ACCESS_DENIED", "denied"),
        new ChildNotPresentException()
    };

    private static AuditLog NewAuditLog(string action, string result) => new()
    {
        Action = action,
        EntityType = "Camera",
        Result = result,
        CreatedAtUtc = new DateTime(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc)
    };
}
