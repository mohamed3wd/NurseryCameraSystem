using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurseryCamera.Domain.Entities;

namespace NurseryCamera.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedOnAdd();

        builder.Property(a => a.Action)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(a => a.EntityType)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(a => a.EntityId)
            .HasMaxLength(128);

        builder.Property(a => a.Result)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(a => a.IpHash)
            .HasMaxLength(128);

        builder.Property(a => a.MetadataJson)
            .HasColumnType("nvarchar(max)");

        // Spec section 30: AuditLogs.UserId / CreatedAtUtc / Action
        builder.HasIndex(a => a.UserId);
        builder.HasIndex(a => a.CreatedAtUtc);
        builder.HasIndex(a => a.Action);
    }
}
