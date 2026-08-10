namespace NurseryCamera.Application.Features.Administration.Dtos;

public sealed record AuditLogDto(
    long Id,
    Guid? UserId,
    string Action,
    string EntityType,
    string? EntityId,
    string Result,
    DateTime CreatedAtUtc,
    string? MetadataJson);
