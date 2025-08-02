using Microsoft.AspNetCore.SignalR;

namespace CameraFeed.API.Video;

/// <summary>
/// Represents a SignalR hub for managing camera-related client connections and group memberships.
/// </summary>
/// <remarks>The <see cref="CameraHub"/> class provides functionality for handling client connections and
/// disconnections, as well as managing group memberships for connected clients. It is designed to be used in real-time
/// applications where clients can join or leave specific groups to receive targeted messages.</remarks>
public class CameraHub(ILogger<CameraHub> logger) : Hub
{
    private readonly ILogger<CameraHub> _logger = logger;

    /// <summary>
    /// Called when a new client connects to the hub.
    /// </summary>
    /// <remarks>This method is invoked automatically by the SignalR framework when a client establishes a
    /// connection. Override this method to execute custom logic when a client connects, such as logging or initializing
    /// resources.</remarks>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
    public override Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects from the hub.
    /// </summary>
    /// <remarks>This method is invoked automatically by the SignalR framework when a client disconnects. 
    /// Override this method to add custom logic to handle disconnections, such as logging or cleanup.</remarks>
    /// <param name="exception">The exception that occurred during the disconnection, if any; otherwise, <see langword="null"/>.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
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
