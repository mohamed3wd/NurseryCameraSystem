using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurseryCamera.Domain.Entities;

namespace NurseryCamera.Infrastructure.Persistence.Configurations;

public sealed class SecurityEventConfiguration : IEntityTypeConfiguration<SecurityEvent>
{
    public void Configure(EntityTypeBuilder<SecurityEvent> builder)
    {
        builder.ToTable("SecurityEvents");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedOnAdd();

        builder.Property(s => s.EventType)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(s => s.Severity)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(s => s.IpHash)
            .HasMaxLength(128);

        builder.Property(s => s.DeviceIdHash)
            .HasMaxLength(128);

        builder.Property(s => s.MetadataJson)
            .HasColumnType("nvarchar(max)");

        // Spec section 30: SecurityEvents.CreatedAtUtc / EventType
        builder.HasIndex(s => s.CreatedAtUtc);
        builder.HasIndex(s => s.EventType);
    }
}
