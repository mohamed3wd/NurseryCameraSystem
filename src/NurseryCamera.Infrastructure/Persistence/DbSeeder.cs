using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NurseryCamera.Application.Abstractions.Security;
using NurseryCamera.Domain.Entities;
using NurseryCamera.Domain.Enums;
using NurseryCamera.Infrastructure.Identity;

namespace NurseryCamera.Infrastructure.Persistence;

/// <summary>
/// Seeds a minimal local demo dataset: nursery, admin, parent, child, room, camera
/// (with encrypted fake RTSP credentials), and the parent-child link. Intended for
/// local development only; call explicitly from the API startup pipeline.
/// </summary>
public static class DbSeeder
{
    public static readonly string[] Roles = { "Admin", "Staff", "Parent" };

    public static async Task SeedAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var provider = scope.ServiceProvider;

        var dbContext = provider.GetRequiredService<AppDbContext>();
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = provider.GetRequiredService<RoleManager<ApplicationRole>>();
        var encryptionService = provider.GetRequiredService<ISecretEncryptionService>();
        var logger = provider.GetRequiredService<ILogger<AppDbContext>>();

        // Applies pending EF Core migrations. Requires the InitialCreate migration
        // (or later) to already exist; run `dotnet ef database update` otherwise.
        await dbContext.Database.MigrateAsync(cancellationToken);

        foreach (var roleName in Roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new ApplicationRole(roleName));
            }
        }

        if (dbContext.Nurseries.Any())
        {
            logger.LogInformation("Database already seeded; skipping demo data seed.");
            return;
        }

        var now = DateTime.UtcNow;

        var nursery = new Nursery
        {
            Id = Guid.NewGuid(),
            Name = "Sunshine Demo Nursery",
            TimeZoneId = "UTC",
            Address = "1 Demo Street",
            IsActive = true,
            CreatedAtUtc = now
        };
        dbContext.Nurseries.Add(nursery);

        var room = new Room
        {
            Id = Guid.NewGuid(),
            NurseryId = nursery.Id,
            Name = "Caterpillar Room",
            Code = "ROOM-1",
            RoomType = "Toddler",
            IsActive = true
        };
        dbContext.Rooms.Add(room);

        // NEVER a real camera: encrypted, non-routable placeholder for local demo only.
        var camera = new Camera
        {
            Id = Guid.NewGuid(),
            NurseryId = nursery.Id,
            Name = "Caterpillar Room Camera 1",
            Location = "Ceiling - North Corner",
            RtspUrlEncrypted = encryptionService.Encrypt("rtsp://192.0.2.10:554/demo-stream"),
            UsernameEncrypted = encryptionService.Encrypt("demo-camera-user"),
            PasswordEncrypted = encryptionService.Encrypt("demo-camera-password"),
            Status = CameraStatus.ACTIVE,
            StreamProfile = "main",
            IsActive = true,
            LastHealthCheckUtc = now
        };
        dbContext.Cameras.Add(camera);

        dbContext.CameraRooms.Add(new CameraRoom
        {
            CameraId = camera.Id,
            RoomId = room.Id,
            ValidFromUtc = now
        });

        var adminUser = await CreateUserIfNotExistsAsync(
            userManager, "admin@demo-nursery.local", "Admin User", "Passw0rd!123", "Admin", logger);

        var parentUser = await CreateUserIfNotExistsAsync(
            userManager, "parent@demo-nursery.local", "Demo Parent", "Passw0rd!123", "Parent", logger);

        var staff = new Staff
        {
            Id = Guid.NewGuid(),
            UserId = adminUser.Id,
            NurseryId = nursery.Id,
            EmployeeNumber = "EMP-0001",
            Role = StaffRole.Admin,
            IsActive = true
        };
        dbContext.Staff.Add(staff);

        var parent = new Parent
        {
            Id = Guid.NewGuid(),
            UserId = parentUser.Id,
            Status = ParentStatus.Active
        };
        dbContext.Parents.Add(parent);

        var child = new Child
        {
            Id = Guid.NewGuid(),
            NurseryId = nursery.Id,
            RoomId = room.Id,
            FirstName = "Demo",
            LastName = "Child",
            DateOfBirth = DateOnly.FromDateTime(now.AddYears(-2)),
            EnrollmentStatus = EnrollmentStatus.Active,
            IsActive = true
        };
        dbContext.Children.Add(child);

        dbContext.ParentChildren.Add(new ParentChild
        {
            ParentId = parent.Id,
            ChildId = child.Id,
            RelationshipType = "Parent",
            IsPrimary = true,
            CanViewCamera = true,
            CreatedAtUtc = now
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seeded demo nursery {NurseryName} with id {NurseryId}.", nursery.Name, nursery.Id);
    }

    private static async Task<ApplicationUser> CreateUserIfNotExistsAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string fullName,
        string password,
        string role,
        ILogger logger)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            return existing;
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = fullName,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            logger.LogError(
                "Failed to seed demo user {Email}: {Errors}",
                email,
                string.Join(", ", result.Errors.Select(e => e.Description)));
            throw new InvalidOperationException($"Failed to seed demo user {email}.");
        }

        await userManager.AddToRoleAsync(user, role);
        return user;
    }
}
