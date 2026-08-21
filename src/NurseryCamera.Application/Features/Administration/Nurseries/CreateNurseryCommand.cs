using FluentValidation;
using MediatR;
using NurseryCamera.Application.Abstractions.Persistence;
using NurseryCamera.Application.Abstractions.Time;
using NurseryCamera.Application.Features.Administration.Dtos;
using NurseryCamera.Domain.Entities;

namespace NurseryCamera.Application.Features.Administration.Nurseries;

public sealed record CreateNurseryCommand(
    string Name,
    string TimeZoneId,
    string? Address) : IRequest<NurseryDto>;

public sealed class CreateNurseryCommandValidator : AbstractValidator<CreateNurseryCommand>
{
    public CreateNurseryCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.TimeZoneId).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Address).MaximumLength(500);
    }
}

public sealed class CreateNurseryCommandHandler : IRequestHandler<CreateNurseryCommand, NurseryDto>
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;

    public CreateNurseryCommandHandler(IAppDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public Task<NurseryDto> Handle(CreateNurseryCommand request, CancellationToken cancellationToken)
    {
        var nursery = new Nursery
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            TimeZoneId = request.TimeZoneId.Trim(),
            Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim(),
            IsActive = true,
            CreatedAtUtc = _clock.UtcNow
        };

        _db.Nurseries.Add(nursery);

        return Task.FromResult(new NurseryDto(
            nursery.Id,
            nursery.Name,
            nursery.TimeZoneId,
            nursery.Address,
            nursery.IsActive));
    }
}
