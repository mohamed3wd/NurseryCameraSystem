using FluentValidation;
using MediatR;
using NurseryCamera.Application.Abstractions.Identity;
using NurseryCamera.Application.Features.Auth.Dtos;

namespace NurseryCamera.Application.Features.Auth.Commands;

public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResponse>;

public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}

public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    private readonly IAuthService _authService;

    public RefreshTokenCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public Task<AuthResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        => _authService.RefreshAsync(request.RefreshToken, cancellationToken);
}
