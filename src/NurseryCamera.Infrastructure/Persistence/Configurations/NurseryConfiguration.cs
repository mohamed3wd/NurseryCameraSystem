using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurseryCamera.Domain.Entities;

namespace NurseryCamera.Infrastructure.Persistence.Configurations;

public sealed class NurseryConfiguration : IEntityTypeConfiguration<Nursery>
{
    public void Configure(EntityTypeBuilder<Nursery> builder)
    {
        builder.ToTable("Nurseries");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(n => n.TimeZoneId)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(n => n.Address)
            .HasMaxLength(512);

        builder.HasIndex(n => n.IsActive);
    }
}
