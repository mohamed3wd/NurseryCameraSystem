using FluentAssertions;
using Moq;
using NurseryCamera.Application.Abstractions.Time;
using NurseryCamera.Application.Features.Cameras.Policies;
using NurseryCamera.Domain.Enums;
using NurseryCamera.UnitTests.Helpers;

namespace NurseryCamera.UnitTests.Features.Cameras;

public sealed class CameraAccessPolicyTests
{
    private readonly DateTime _utcNow = new(2026, 8, 10, 10, 0, 0, DateTimeKind.Utc);

    private (CameraAccessPolicy Policy, CameraAccessSeed Seed, Infrastructure.Persistence.AppDbContext Db) CreateSut()
    {
        var db = InMemoryDbFactory.Create();
        var seed = CameraAccessSeed.CreateAuthorizedAsync(db, _utcNow).GetAwaiter().GetResult();

        var clock = new Mock<IClock>();
        clock.Setup(c => c.UtcNow).Returns(_utcNow);

        var policy = new CameraAccessPolicy(db, clock.Object);
        return (policy, seed, db);
    }

    [Fact]
    public async Task CanViewAsync_Denies_WhenNoParentChildLink()
    {
        var (policy, seed, db) = CreateSut();
        db.ParentChildren.Remove(seed.Relation);
        await db.SaveChangesAsync();

        var decision = await policy.CanViewAsync(seed.UserId, seed.ChildId, seed.CameraId, CancellationToken.None);

        decision.Allowed.Should().BeFalse();
        decision.DenialCode.Should().Be("PARENT_CHILD_RELATION_NOT_FOUND");
    }

    [Fact]
    public async Task CanViewAsync_Denies_WhenCanViewCameraIsFalse()
    {
        var (policy, seed, db) = CreateSut();
        seed.Relation.CanViewCamera = false;
        await db.SaveChangesAsync();

        var decision = await policy.CanViewAsync(seed.UserId, seed.ChildId, seed.CameraId, CancellationToken.None);

        decision.Allowed.Should().BeFalse();
        decision.DenialCode.Should().Be("CAMERA_ACCESS_DENIED");
        decision.DenialMessage.Should().Contain("not been enabled");
    }

    [Fact]
    public async Task CanViewAsync_Denies_WhenChildNotPresent()
    {
        var (policy, seed, db) = CreateSut();
        seed.Attendance.Status = AttendanceStatus.COMPLETED;
        seed.Attendance.CheckOutUtc = _utcNow;
        await db.SaveChangesAsync();

        var decision = await policy.CanViewAsync(seed.UserId, seed.ChildId, seed.CameraId, CancellationToken.None);

        decision.Allowed.Should().BeFalse();
        decision.DenialCode.Should().Be("CHILD_NOT_PRESENT");
    }

    [Fact]
    public async Task CanViewAsync_Denies_WhenCameraNotAssignedToChildRoom()
    {
        var (policy, seed, db) = CreateSut();
        db.CameraRooms.Remove(seed.CameraRoom);
        await db.SaveChangesAsync();

        var decision = await policy.CanViewAsync(seed.UserId, seed.ChildId, seed.CameraId, CancellationToken.None);

        decision.Allowed.Should().BeFalse();
        decision.DenialCode.Should().Be("CAMERA_ACCESS_DENIED");
        decision.DenialMessage.Should().Contain("not assigned");
    }

    [Fact]
    public async Task CanViewAsync_Denies_WhenCameraInactive()
    {
        var (policy, seed, db) = CreateSut();
        seed.Camera.IsActive = false;
        seed.Camera.Status = CameraStatus.INACTIVE;
        await db.SaveChangesAsync();

        var decision = await policy.CanViewAsync(seed.UserId, seed.ChildId, seed.CameraId, CancellationToken.None);

        decision.Allowed.Should().BeFalse();
        decision.DenialCode.Should().Be("CAMERA_NOT_AVAILABLE");
    }

    [Fact]
    public async Task CanViewAsync_Allows_WhenAllConditionsMet()
    {
        var (policy, seed, _) = CreateSut();

        var decision = await policy.CanViewAsync(seed.UserId, seed.ChildId, seed.CameraId, CancellationToken.None);

        decision.Allowed.Should().BeTrue();
        decision.DenialCode.Should().BeNull();
        decision.DenialMessage.Should().BeNull();
        decision.ParentId.Should().Be(seed.ParentId);
        decision.AttendanceSessionId.Should().Be(seed.AttendanceSessionId);
    }
}
