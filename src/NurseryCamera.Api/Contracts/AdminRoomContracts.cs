namespace NurseryCamera.Api.Contracts;

public sealed record CreateRoomRequest(Guid NurseryId, string Name, string Code, string? RoomType);
