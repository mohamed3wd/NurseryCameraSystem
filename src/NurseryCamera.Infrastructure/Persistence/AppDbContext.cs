using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NurseryCamera.Application.Abstractions.Persistence;
using NurseryCamera.Domain.Entities;
using NurseryCamera.Infrastructure.Identity;
using NurseryCamera.Infrastructure.Persistence.Configurations;

namespace NurseryCamera.Infrastructure.Persistence;

public class AppDbContext
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Nursery> Nurseries => Set<Nursery>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Child> Children => Set<Child>();
    public DbSet<Parent> Parents => Set<Parent>();
    public DbSet<ParentChild> ParentChildren => Set<ParentChild>();
    public DbSet<Staff> Staff => Set<Staff>();
    public DbSet<Camera> Cameras => Set<Camera>();
    public DbSet<CameraRoom> CameraRooms => Set<CameraRoom>();
    public DbSet<AttendanceSession> AttendanceSessions => Set<AttendanceSession>();
    public DbSet<ViewingSession> ViewingSessions => Set<ViewingSession>();
    public DbSet<StreamToken> StreamTokens => Set<StreamToken>();
    public DbSet<CameraHealthCheck> CameraHealthChecks => Set<CameraHealthCheck>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<SecurityEvent> SecurityEvents => Set<SecurityEvent>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Rename ASP.NET Core Identity tables to match the ERD naming (spec section 6).
        builder.Entity<ApplicationUser>().ToTable("Users");
        builder.Entity<ApplicationRole>().ToTable("Roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");

        builder.ApplyConfiguration(new ApplicationUserConfiguration());
        builder.ApplyConfiguration(new NurseryConfiguration());
        builder.ApplyConfiguration(new RoomConfiguration());
        builder.ApplyConfiguration(new ChildConfiguration());
        builder.ApplyConfiguration(new ParentConfiguration());
        builder.ApplyConfiguration(new ParentChildConfiguration());
        builder.ApplyConfiguration(new StaffConfiguration());
        builder.ApplyConfiguration(new CameraConfiguration());
        builder.ApplyConfiguration(new CameraRoomConfiguration());
        builder.ApplyConfiguration(new AttendanceSessionConfiguration());
        builder.ApplyConfiguration(new ViewingSessionConfiguration());
        builder.ApplyConfiguration(new StreamTokenConfiguration());
        builder.ApplyConfiguration(new CameraHealthCheckConfiguration());
        builder.ApplyConfiguration(new AuditLogConfiguration());
        builder.ApplyConfiguration(new SecurityEventConfiguration());
        builder.ApplyConfiguration(new OutboxMessageConfiguration());
    }
}
