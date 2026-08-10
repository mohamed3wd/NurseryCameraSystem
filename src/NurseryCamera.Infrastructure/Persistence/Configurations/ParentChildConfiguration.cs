using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurseryCamera.Domain.Entities;

namespace NurseryCamera.Infrastructure.Persistence.Configurations;

public sealed class ParentChildConfiguration : IEntityTypeConfiguration<ParentChild>
{
    public void Configure(EntityTypeBuilder<ParentChild> builder)
    {
        builder.ToTable("ParentChildren");

        // Composite primary key: ParentId + ChildId (spec section 7).
        builder.HasKey(pc => new { pc.ParentId, pc.ChildId });

        builder.Property(pc => pc.RelationshipType)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasOne(pc => pc.Parent)
            .WithMany(p => p.ParentChildren)
            .HasForeignKey(pc => pc.ParentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pc => pc.Child)
            .WithMany(c => c.ParentChildren)
            .HasForeignKey(pc => pc.ChildId)
            .OnDelete(DeleteBehavior.Cascade);

        // Spec section 30: ParentChildren.ParentId / ChildId / composite UNIQUE.
        // The composite PK above already enforces uniqueness on (ParentId, ChildId)
        // and serves as an index on ParentId; add explicit indexes for both directions.
        builder.HasIndex(pc => pc.ParentId);
        builder.HasIndex(pc => pc.ChildId);
    }
}
