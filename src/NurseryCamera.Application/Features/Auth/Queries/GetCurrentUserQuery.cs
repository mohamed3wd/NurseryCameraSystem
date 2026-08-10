using MediatR;
using NurseryCamera.Application.Abstractions.Identity;
using NurseryCamera.Application.Common.Exceptions;
using NurseryCamera.Application.Features.Auth.Dtos;

namespace NurseryCamera.Application.Features.Auth.Queries;

public sealed record GetCurrentUserQuery : IRequest<UserDto>;

public sealed class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, UserDto>
{
    private readonly ICurrentUser _currentUser;
    private readonly IAuthService _authService;

    public GetCurrentUserQueryHandler(ICurrentUser currentUser, IAuthService authService)
    {
        _currentUser = currentUser;
        _authService = authService;
    }

    public Task<UserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            throw AppException.AuthenticationRequired();
        }

        return _authService.GetMeAsync(userId, cancellationToken);
    }
}
