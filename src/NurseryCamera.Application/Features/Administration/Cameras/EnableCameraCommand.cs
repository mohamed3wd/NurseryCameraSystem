using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NurseryCamera.Application.Abstractions.Audit;
using NurseryCamera.Application.Abstractions.Identity;
using NurseryCamera.Application.Abstractions.Persistence;
using NurseryCamera.Application.Common.Exceptions;
using NurseryCamera.Domain.Enums;

namespace NurseryCamera.Application.Features.Administration.Cameras;

public sealed record EnableCameraCommand(Guid CameraId) : IRequest;

public sealed class EnableCameraCommandValidator : AbstractValidator<EnableCameraCommand>
{
    public EnableCameraCommandValidator() => RuleFor(x => x.CameraId).NotEmpty();
}

public sealed class EnableCameraCommandHandler : IRequestHandler<EnableCameraCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditService _auditService;

    public EnableCameraCommandHandler(IAppDbContext db, ICurrentUser currentUser, IAuditService auditService)
    {
        _db = db;
        _currentUser = currentUser;
        _auditService = auditService;
    }

    public async Task Handle(EnableCameraCommand request, CancellationToken cancellationToken)
    {
        var camera = await _db.Cameras.FirstOrDefaultAsync(c => c.Id == request.CameraId, cancellationToken)
                     ?? throw AppException.NotFound("CAMERA_NOT_FOUND", "Camera not found.");

        camera.IsActive = true;
        camera.Status = CameraStatus.ACTIVE;

        await _auditService.LogAsync(
            new AuditEvent(_currentUser.UserId, "CAMERA_ENABLED", "Camera", camera.Id.ToString(), "SUCCESS"),
            cancellationToken);
    }
}
