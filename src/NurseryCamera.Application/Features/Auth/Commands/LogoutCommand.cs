using MediatR;
using NurseryCamera.Application.Abstractions.Audit;
using NurseryCamera.Application.Abstractions.Identity;
using NurseryCamera.Application.Common.Exceptions;

namespace NurseryCamera.Application.Features.Auth.Commands;

public sealed record LogoutCommand(string? RefreshToken) : IRequest;

public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly IAuthService _authService;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditService _auditService;

    public LogoutCommandHandler(IAuthService authService, ICurrentUser currentUser, IAuditService auditService)
    {
        _authService = authService;
        _currentUser = currentUser;
        _auditService = auditService;
    }

    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            throw AppException.AuthenticationRequired();
        }

        await _authService.LogoutAsync(userId, request.RefreshToken, cancellationToken);
        await _auditService.LogAsync(new AuditEvent(userId, "LOGOUT", "User", userId.ToString(), "SUCCESS"), cancellationToken);
    }
}
