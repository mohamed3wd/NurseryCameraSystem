using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NurseryCamera.Application.Abstractions.Audit;
using NurseryCamera.Application.Abstractions.Identity;
using NurseryCamera.Application.Abstractions.Persistence;
using NurseryCamera.Application.Abstractions.Security;
using NurseryCamera.Application.Common.Exceptions;
using NurseryCamera.Application.Features.Administration.Dtos;
using NurseryCamera.Domain.Entities;
using NurseryCamera.Domain.Enums;

namespace NurseryCamera.Application.Features.Administration.Cameras;

/// <summary>Admin-only. Camera secrets are encrypted before persistence and never logged (BR-015/BR-016).</summary>
public sealed record CreateCameraCommand(
    Guid NurseryId,
    string Name,
    string? Location,
    string RtspUrl,
    string? Username,
    string? Password,
    string? StreamProfile) : IRequest<CameraAdminDto>;

public sealed class CreateCameraCommandValidator : AbstractValidator<CreateCameraCommand>
{
    public CreateCameraCommandValidator()
    {
        RuleFor(x => x.NurseryId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Location).MaximumLength(200);
        RuleFor(x => x.RtspUrl).NotEmpty();
        RuleFor(x => x.StreamProfile).MaximumLength(100);
    }
}

public sealed class CreateCameraCommandHandler : IRequestHandler<CreateCameraCommand, CameraAdminDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ISecretEncryptionService _encryptionService;
    private readonly IAuditService _auditService;

    public CreateCameraCommandHandler(
        IAppDbContext db,
        ICurrentUser currentUser,
        ISecretEncryptionService encryptionService,
        IAuditService auditService)
    {
        _db = db;
        _currentUser = currentUser;
        _encryptionService = encryptionService;
        _auditService = auditService;
    }

    public async Task<CameraAdminDto> Handle(CreateCameraCommand request, CancellationToken cancellationToken)
    {
        var nurseryExists = await _db.Nurseries.AsNoTracking().AnyAsync(n => n.Id == request.NurseryId, cancellationToken);
        if (!nurseryExists)
        {
            throw AppException.NotFound("NURSERY_NOT_FOUND", "Nursery not found.");
        }

        var camera = new Camera
        {
            Id = Guid.NewGuid(),
            NurseryId = request.NurseryId,
            Name = request.Name,
            Location = request.Location,
            RtspUrlEncrypted = _encryptionService.Encrypt(request.RtspUrl),
            UsernameEncrypted = _encryptionService.Encrypt(request.Username ?? string.Empty),
            PasswordEncrypted = _encryptionService.Encrypt(request.Password ?? string.Empty),
            Status = CameraStatus.INACTIVE,
            StreamProfile = request.StreamProfile,
            IsActive = true
        };

        _db.Cameras.Add(camera);
        await _db.SaveChangesAsync(cancellationToken);

        // Metadata intentionally excludes RtspUrl/Username/Password (BR-016).
        await _auditService.LogAsync(
            new AuditEvent(_currentUser.UserId, "CAMERA_CREATED", "Camera", camera.Id.ToString(), "SUCCESS",
                Metadata: new { camera.Name, camera.NurseryId }),
            cancellationToken);

        return new CameraAdminDto(camera.Id, camera.NurseryId, camera.Name, camera.Location, camera.Status.ToString(),
            camera.StreamProfile, camera.IsActive, camera.LastHealthCheckUtc, Array.Empty<Guid>());
    }
}
