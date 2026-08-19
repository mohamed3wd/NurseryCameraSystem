using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurseryCamera.Domain.Entities;

namespace NurseryCamera.Infrastructure.Persistence.Configurations;

public sealed class StreamTokenConfiguration : IEntityTypeConfiguration<StreamToken>
{
    public void Configure(EntityTypeBuilder<StreamToken> builder)
    {
        builder.ToTable("StreamTokens");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.TokenHash)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.HasOne(t => t.ViewingSession)
            .WithMany(v => v.StreamTokens)
            .HasForeignKey(t => t.ViewingSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Spec section 30: StreamTokens.ViewingSessionId / StreamTokens.ExpiresAtUtc
        builder.HasIndex(t => t.ViewingSessionId);
        builder.HasIndex(t => t.ExpiresAtUtc);

        // Matches TokenCleanupWorker's "ACTIVE and lapsed" sweep and its retention delete.
        builder.HasIndex(t => new { t.Status, t.ExpiresAtUtc });

        // Raw tokens are never stored (spec section 14); the hash must be unique so
        // authorization lookups by hash are unambiguous.
        builder.HasIndex(t => t.TokenHash).IsUnique();
    }
}
