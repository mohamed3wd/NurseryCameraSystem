using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurseryCamera.Domain.Entities;

namespace NurseryCamera.Infrastructure.Persistence.Configurations;

public sealed class ChildConfiguration : IEntityTypeConfiguration<Child>
{
    public void Configure(EntityTypeBuilder<Child> builder)
    {
        builder.ToTable("Children");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.FirstName)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(c => c.LastName)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(c => c.EnrollmentStatus)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.HasOne(c => c.Nursery)
            .WithMany(n => n.Children)
            .HasForeignKey(c => c.NurseryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Room)
            .WithMany(r => r.Children)
            .HasForeignKey(c => c.RoomId)
            .OnDelete(DeleteBehavior.SetNull);

        // Spec section 30: Children.NurseryId / Children.RoomId / Children.IsActive
        builder.HasIndex(c => c.NurseryId);
        builder.HasIndex(c => c.RoomId);
        builder.HasIndex(c => c.IsActive);
    }
}
