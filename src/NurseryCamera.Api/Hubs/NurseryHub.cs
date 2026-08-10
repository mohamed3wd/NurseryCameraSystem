using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace NurseryCamera.Api.Hubs;

/// <summary>
/// Real-time push channel for parent/staff clients (spec section 22). Every connection is
/// authenticated (JWT bearer, same as the REST API) and placed into a group named after the
/// caller's user id so server-side code can target a specific user with
/// <c>Clients.Group(userId)</c> for ChildCheckedIn, ChildCheckedOut, ViewingSessionRevoked,
/// CameraStatusChanged, and NotificationCreated events. As documented on
/// <see cref="NurseryCamera.Application.Abstractions.Notifications.INotificationService"/>,
/// this is a courtesy signal only - it never substitutes for server-side authorization.
/// </summary>
[Authorize]
public sealed class NurseryHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupNameForUser(userId));
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupNameForUser(userId));
        }

        await base.OnDisconnectedAsync(exception);
    }

    public static string GroupNameForUser(string userId) => $"user:{userId}";

    public static string GroupNameForUser(Guid userId) => GroupNameForUser(userId.ToString());
}

/// <summary>Client event names pushed over <see cref="NurseryHub"/>. Keep in sync with the frontend.</summary>
public static class NurseryHubEvents
{
    public const string ChildCheckedIn = "ChildCheckedIn";
    public const string ChildCheckedOut = "ChildCheckedOut";
    public const string ViewingSessionRevoked = "ViewingSessionRevoked";
    public const string CameraStatusChanged = "CameraStatusChanged";
    public const string NotificationCreated = "NotificationCreated";
}
