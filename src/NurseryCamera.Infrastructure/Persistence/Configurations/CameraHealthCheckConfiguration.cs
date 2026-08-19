using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurseryCamera.Domain.Entities;

namespace NurseryCamera.Infrastructure.Persistence.Configurations;

public sealed class CameraHealthCheckConfiguration : IEntityTypeConfiguration<CameraHealthCheck>
{
    public void Configure(EntityTypeBuilder<CameraHealthCheck> builder)
    {
        builder.ToTable("CameraHealthChecks");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.Status)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(h => h.ErrorCode)
            .HasMaxLength(64);

        builder.HasOne(h => h.Camera)
            .WithMany(c => c.HealthChecks)
            .HasForeignKey(h => h.CameraId)
            .OnDelete(DeleteBehavior.Cascade);

        // Spec section 30: CameraHealthChecks.CameraId + CheckedAtUtc
        builder.HasIndex(h => new { h.CameraId, h.CheckedAtUtc });

        // Retention pruning deletes by age across all cameras, which the composite index above
        // cannot seek on.
        builder.HasIndex(h => h.CheckedAtUtc);
    }
}
