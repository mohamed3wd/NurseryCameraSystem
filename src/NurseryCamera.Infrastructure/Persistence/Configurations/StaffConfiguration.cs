using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurseryCamera.Domain.Entities;

namespace NurseryCamera.Infrastructure.Persistence.Configurations;

public sealed class StaffConfiguration : IEntityTypeConfiguration<Staff>
{
    public void Configure(EntityTypeBuilder<Staff> builder)
    {
        builder.ToTable("Staff");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.EmployeeNumber)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(s => s.Role)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.HasOne(s => s.Nursery)
            .WithMany(n => n.Staff)
            .HasForeignKey(s => s.NurseryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => s.UserId).IsUnique();
        builder.HasIndex(s => s.NurseryId);
        builder.HasIndex(s => new { s.NurseryId, s.EmployeeNumber }).IsUnique();
    }
}
