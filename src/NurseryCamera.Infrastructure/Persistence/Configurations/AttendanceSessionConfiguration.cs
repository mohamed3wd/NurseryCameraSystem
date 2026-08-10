using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurseryCamera.Domain.Entities;
using NurseryCamera.Domain.Enums;

namespace NurseryCamera.Infrastructure.Persistence.Configurations;

public sealed class AttendanceSessionConfiguration : IEntityTypeConfiguration<AttendanceSession>
{
    public void Configure(EntityTypeBuilder<AttendanceSession> builder)
    {
        builder.ToTable("AttendanceSessions");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(a => a.Source)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(a => a.Notes)
            .HasMaxLength(1024);

        builder.HasOne(a => a.Child)
            .WithMany(c => c.AttendanceSessions)
            .HasForeignKey(a => a.ChildId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Staff)
            .WithMany(s => s.AttendanceSessions)
            .HasForeignKey(a => a.StaffId)
            .OnDelete(DeleteBehavior.SetNull);

        // Spec section 30: AttendanceSessions.ChildId + Status / ChildId + CheckInUtc
        builder.HasIndex(a => new { a.ChildId, a.Status });
        builder.HasIndex(a => new { a.ChildId, a.CheckInUtc });

        // Invariant (spec section 7): at most one active PRESENT attendance session per child.
        // Filtered unique index enforces this at the database level, closing the race window
        // described in spec section 31 (concurrency rules).
        builder.HasIndex(a => a.ChildId)
            .IsUnique()
            .HasDatabaseName("IX_AttendanceSessions_ChildId_OnePresent")
            .HasFilter($"[Status] = '{nameof(AttendanceStatus.PRESENT)}'");
    }
}
