using Microsoft.AspNetCore.SignalR;

namespace CameraFeed.API.Video;

/// <summary>
/// Represents a SignalR hub for managing camera-related client connections and group memberships.
/// </summary>
/// <remarks>The <see cref="CameraHub"/> class provides functionality for handling client connections and
/// disconnections, as well as managing group memberships for connected clients. It is designed to be used in real-time
/// applications where clients can join or leave specific groups to receive targeted messages.</remarks>
public class CameraHub : Hub
{
    public override Task OnConnectedAsync()
    {
        Console.WriteLine("Client connected: " + Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        Console.WriteLine("Client disconnected: " + Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Adds the current connection to the specified group.
    /// </summary>
    /// <remarks>This method associates the connection identified by <see cref="Context.ConnectionId"/> with
    /// the specified group. Once added, the connection will receive messages sent to the group.</remarks>
    /// <param name="groupName">The name of the group to join. Cannot be null or empty.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }

    /// <summary>
    /// Removes the current connection from the specified group.
    /// </summary>
    /// <remarks>This method removes the connection associated with the current context from the specified
    /// group. Groups are used to manage connections collectively, enabling broadcasting messages to all members of a
    /// group. Ensure that <paramref name="groupName"/> is valid and corresponds to an existing group.</remarks>
    /// <param name="groupName">The name of the group to leave. Cannot be null or empty.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task LeaveGroup(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }
}
