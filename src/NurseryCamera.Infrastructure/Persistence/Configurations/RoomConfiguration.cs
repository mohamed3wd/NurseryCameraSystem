using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurseryCamera.Domain.Entities;

namespace NurseryCamera.Infrastructure.Persistence.Configurations;

public sealed class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.ToTable("Rooms");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(r => r.Code)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(r => r.RoomType)
            .HasMaxLength(64);

        builder.HasOne(r => r.Nursery)
            .WithMany(n => n.Rooms)
            .HasForeignKey(r => r.NurseryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => r.NurseryId);
        builder.HasIndex(r => new { r.NurseryId, r.Code }).IsUnique();
    }
}
