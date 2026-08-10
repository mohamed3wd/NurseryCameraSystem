using FluentValidation;
using MediatR;
using NurseryCamera.Application.Abstractions.Audit;
using NurseryCamera.Application.Abstractions.Identity;
using NurseryCamera.Application.Common.Exceptions;
using NurseryCamera.Application.Features.Auth.Dtos;

namespace NurseryCamera.Application.Features.Auth.Commands;

public sealed record LoginCommand(string Email, string Password) : IRequest<AuthResponse>;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponse>
{
    private readonly IAuthService _authService;
    private readonly IAuditService _auditService;

    public LoginCommandHandler(IAuthService authService, IAuditService auditService)
    {
        _authService = authService;
        _auditService = auditService;
    }

    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _authService.LoginAsync(request.Email, request.Password, cancellationToken);

            await _auditService.LogAsync(
                new AuditEvent(response.User.Id, "LOGIN_SUCCESS", "User", response.User.Id.ToString(), "SUCCESS"),
                cancellationToken);

            return response;
        }
        catch (AppException)
        {
            await _auditService.LogAsync(
                new AuditEvent(null, "LOGIN_FAILED", "User", null, "FAILURE", Metadata: new { request.Email }),
                cancellationToken);
            throw;
        }
    }
}
