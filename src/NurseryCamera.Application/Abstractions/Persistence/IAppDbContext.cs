using Microsoft.EntityFrameworkCore;
using NurseryCamera.Domain.Entities;

namespace NurseryCamera.Application.Abstractions.Persistence;

/// <summary>
/// Application-facing view of the EF Core database context. Keeps handlers decoupled from
/// the concrete Infrastructure DbContext implementation while still allowing efficient
/// server-side LINQ queries (per spec section 12, camera scope must be enforced in SQL,
/// never filtered client-side).
/// </summary>
public interface IAppDbContext
{
    DbSet<Nursery> Nurseries { get; }
    DbSet<Room> Rooms { get; }
    DbSet<Child> Children { get; }
    DbSet<Parent> Parents { get; }
    DbSet<ParentChild> ParentChildren { get; }
    DbSet<Staff> Staff { get; }
    DbSet<Camera> Cameras { get; }
    DbSet<CameraRoom> CameraRooms { get; }
    DbSet<AttendanceSession> AttendanceSessions { get; }
    DbSet<ViewingSession> ViewingSessions { get; }
    DbSet<StreamToken> StreamTokens { get; }
    DbSet<CameraHealthCheck> CameraHealthChecks { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<SecurityEvent> SecurityEvents { get; }
    DbSet<OutboxMessage> OutboxMessages { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
