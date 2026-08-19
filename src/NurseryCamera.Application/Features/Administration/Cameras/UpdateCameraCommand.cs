using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NurseryCamera.Application.Abstractions.Audit;
using NurseryCamera.Application.Abstractions.Identity;
using NurseryCamera.Application.Abstractions.Persistence;
using NurseryCamera.Application.Abstractions.Security;
using NurseryCamera.Application.Common.Exceptions;
using NurseryCamera.Application.Features.Administration.Dtos;

namespace NurseryCamera.Application.Features.Administration.Cameras;

public sealed record UpdateCameraCommand(
    Guid CameraId,
    string Name,
    string? Location,
    string? RtspUrl,
    string? Username,
    string? Password,
    string? StreamProfile) : IRequest<CameraAdminDto>;

public sealed class UpdateCameraCommandValidator : AbstractValidator<UpdateCameraCommand>
{
    public UpdateCameraCommandValidator()
    {
        RuleFor(x => x.CameraId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Location).MaximumLength(200);
        RuleFor(x => x.StreamProfile).MaximumLength(100);
    }
}

public sealed class UpdateCameraCommandHandler : IRequestHandler<UpdateCameraCommand, CameraAdminDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ISecretEncryptionService _encryptionService;
    private readonly IAuditService _auditService;

    public UpdateCameraCommandHandler(
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

    public async Task<CameraAdminDto> Handle(UpdateCameraCommand request, CancellationToken cancellationToken)
    {
        var camera = await _db.Cameras.FirstOrDefaultAsync(c => c.Id == request.CameraId, cancellationToken)
                     ?? throw AppException.NotFound("CAMERA_NOT_FOUND", "Camera not found.");

        camera.Name = request.Name;
        camera.Location = request.Location;
        camera.StreamProfile = request.StreamProfile;

        if (!string.IsNullOrEmpty(request.RtspUrl))
        {
            camera.RtspUrlEncrypted = _encryptionService.Encrypt(request.RtspUrl);
        }

        if (request.Username is not null)
        {
            camera.UsernameEncrypted = _encryptionService.Encrypt(request.Username);
        }

        if (request.Password is not null)
        {
            camera.PasswordEncrypted = _encryptionService.Encrypt(request.Password);
        }

        await _auditService.LogAsync(
            new AuditEvent(_currentUser.UserId, "CAMERA_UPDATED", "Camera", camera.Id.ToString(), "SUCCESS",
                Metadata: new { camera.Name }),
            cancellationToken);

        var roomIds = await _db.CameraRooms
            .AsNoTracking()
            .Where(cr => cr.CameraId == camera.Id && cr.ValidToUtc == null)
            .Select(cr => cr.RoomId)
            .ToListAsync(cancellationToken);

        return new CameraAdminDto(camera.Id, camera.NurseryId, camera.Name, camera.Location, camera.Status.ToString(),
            camera.StreamProfile, camera.IsActive, camera.LastHealthCheckUtc, roomIds);
    }
}
