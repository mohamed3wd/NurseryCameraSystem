using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurseryCamera.Domain.Entities;

namespace NurseryCamera.Infrastructure.Persistence.Configurations;

public sealed class CameraRoomConfiguration : IEntityTypeConfiguration<CameraRoom>
{
    public void Configure(EntityTypeBuilder<CameraRoom> builder)
    {
        builder.ToTable("CameraRooms");

        // Composite primary key: CameraId + RoomId (spec section 30 CameraRooms.CameraId + RoomId UNIQUE).
        builder.HasKey(cr => new { cr.CameraId, cr.RoomId });

        builder.HasOne(cr => cr.Camera)
            .WithMany(c => c.CameraRooms)
            .HasForeignKey(cr => cr.CameraId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(cr => cr.Room)
            .WithMany(r => r.CameraRooms)
            .HasForeignKey(cr => cr.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(cr => cr.CameraId);

        // "Which cameras currently cover this room" is the parent-facing camera list query;
        // ValidToUtc is part of it because assignments are closed rather than deleted.
        builder.HasIndex(cr => new { cr.RoomId, cr.ValidToUtc });
    }
}
