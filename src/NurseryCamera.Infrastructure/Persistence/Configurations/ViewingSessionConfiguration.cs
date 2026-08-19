using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurseryCamera.Domain.Entities;

namespace NurseryCamera.Infrastructure.Persistence.Configurations;

public sealed class ViewingSessionConfiguration : IEntityTypeConfiguration<ViewingSession>
{
    public void Configure(EntityTypeBuilder<ViewingSession> builder)
    {
        builder.ToTable("ViewingSessions");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Status)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(v => v.EndReason)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(v => v.ClientType)
            .HasMaxLength(64);

        builder.Property(v => v.DeviceIdHash)
            .HasMaxLength(128);

        builder.HasOne(v => v.Parent)
            .WithMany(p => p.ViewingSessions)
            .HasForeignKey(v => v.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.Child)
            .WithMany(c => c.ViewingSessions)
            .HasForeignKey(v => v.ChildId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.Camera)
            .WithMany(c => c.ViewingSessions)
            .HasForeignKey(v => v.CameraId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.AttendanceSession)
            .WithMany(a => a.ViewingSessions)
            .HasForeignKey(v => v.AttendanceSessionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Spec section 30.
        builder.HasIndex(v => new { v.ParentId, v.Status });
        builder.HasIndex(v => new { v.ChildId, v.Status });
        builder.HasIndex(v => new { v.CameraId, v.Status });
        builder.HasIndex(v => v.ExpiresAtUtc);

        // ViewingSessionExpirationWorker polls "ACTIVE and past ExpiresAtUtc" every few seconds;
        // neither single-column index alone lets that become a seek.
        builder.HasIndex(v => new { v.Status, v.ExpiresAtUtc });
    }
}
