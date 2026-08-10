using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurseryCamera.Domain.Entities;

namespace NurseryCamera.Infrastructure.Persistence.Configurations;

public sealed class CameraConfiguration : IEntityTypeConfiguration<Camera>
{
    public void Configure(EntityTypeBuilder<Camera> builder)
    {
        builder.ToTable("Cameras");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(c => c.Location)
            .HasMaxLength(256);

        // Encrypted secrets: never store/return plaintext. Sized generously for ciphertext + IV/tag.
        builder.Property(c => c.RtspUrlEncrypted)
            .IsRequired()
            .HasMaxLength(2048);

        builder.Property(c => c.UsernameEncrypted)
            .IsRequired()
            .HasMaxLength(1024);

        builder.Property(c => c.PasswordEncrypted)
            .IsRequired()
            .HasMaxLength(1024);

        builder.Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(c => c.StreamProfile)
            .HasMaxLength(64);

        builder.HasOne(c => c.Nursery)
            .WithMany(n => n.Cameras)
            .HasForeignKey(c => c.NurseryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Spec section 30: Cameras.NurseryId / Cameras.Status / Cameras.IsActive
        builder.HasIndex(c => c.NurseryId);
        builder.HasIndex(c => c.Status);
        builder.HasIndex(c => c.IsActive);
    }
}
