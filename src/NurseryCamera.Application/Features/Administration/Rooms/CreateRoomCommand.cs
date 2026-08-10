using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NurseryCamera.Application.Abstractions.Persistence;
using NurseryCamera.Application.Common.Exceptions;
using NurseryCamera.Application.Features.Administration.Dtos;
using NurseryCamera.Domain.Entities;

namespace NurseryCamera.Application.Features.Administration.Rooms;

public sealed record CreateRoomCommand(Guid NurseryId, string Name, string Code, string? RoomType) : IRequest<RoomDto>;

public sealed class CreateRoomCommandValidator : AbstractValidator<CreateRoomCommand>
{
    public CreateRoomCommandValidator()
    {
        RuleFor(x => x.NurseryId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.RoomType).MaximumLength(100);
    }
}

public sealed class CreateRoomCommandHandler : IRequestHandler<CreateRoomCommand, RoomDto>
{
    private readonly IAppDbContext _db;

    public CreateRoomCommandHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<RoomDto> Handle(CreateRoomCommand request, CancellationToken cancellationToken)
    {
        var nurseryExists = await _db.Nurseries.AsNoTracking().AnyAsync(n => n.Id == request.NurseryId, cancellationToken);
        if (!nurseryExists)
        {
            throw AppException.NotFound("NURSERY_NOT_FOUND", "Nursery not found.");
        }

        var room = new Room
        {
            Id = Guid.NewGuid(),
            NurseryId = request.NurseryId,
            Name = request.Name,
            Code = request.Code,
            RoomType = request.RoomType,
            IsActive = true
        };

        _db.Rooms.Add(room);
        await _db.SaveChangesAsync(cancellationToken);

        return new RoomDto(room.Id, room.NurseryId, room.Name, room.Code, room.RoomType, room.IsActive);
    }
}
