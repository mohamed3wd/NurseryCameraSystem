using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurseryCamera.Infrastructure.Identity;

namespace NurseryCamera.Infrastructure.Persistence.Configurations;

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.FullName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(u => u.RefreshToken)
            .HasMaxLength(512);

        // Identity already creates a unique index on NormalizedEmail (Users.Email UNIQUE, spec section 30).
        builder.HasIndex(u => u.IsActive);
    }
}
