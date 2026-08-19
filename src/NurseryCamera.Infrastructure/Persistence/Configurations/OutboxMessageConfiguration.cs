using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurseryCamera.Domain.Entities;

namespace NurseryCamera.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Type)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(o => o.PayloadJson)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(o => o.Error)
            .HasColumnType("nvarchar(max)");

        // Used by OutboxWorker to poll unprocessed messages in order (spec section 33). Filtered
        // to the pending backlog, which stays small while the processed history grows unbounded.
        builder.HasIndex(o => o.OccurredAtUtc)
            .HasFilter("[ProcessedAtUtc] IS NULL");
    }
}
