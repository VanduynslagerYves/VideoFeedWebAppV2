using Microsoft.AspNetCore.SignalR;

namespace CameraFeed.SocketServer.Hubs;

/// <summary>
/// Serves as a base class for SignalR hubs, providing common functionality for managing client connections and group
/// membership.
/// </summary>
/// <remarks>This abstract class extends the <see cref="Hub"/> class and provides additional methods for handling
/// client connections and managing group membership. It includes default implementations for <see
/// cref="OnConnectedAsync"/> and <see cref="OnDisconnectedAsync"/>, which can be overridden to add custom logic for
/// connection and disconnection events. The class also provides utility methods for adding and removing connections
/// from groups. <para> Derived classes should inherit from <see cref="HubBase"/> to leverage its logging capabilities
/// and group management methods. </para></remarks>
/// <param name="logger"></param>
public abstract class HubBase(ILogger<HubBase> logger) : Hub
{
    private readonly ILogger<HubBase> _logger = logger;

    /// <summary>
    /// Called when a new client connects to the hub.
    /// </summary>
    /// <remarks>This method is invoked automatically by the SignalR framework when a client establishes a
    /// connection. Override this method to execute custom logic when a client connects, such as logging or initializing
    /// resources.</remarks>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
    public override Task OnConnectedAsync()
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        _logger.LogInformation("[{Timestamp}] Client connected: {ConnectionId}", timestamp, Context.ConnectionId);
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
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        _logger.LogInformation("[{Timestamp}] Client disconnected: {ConnectionId}", timestamp, Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Adds the current connection to the specified group.
    /// </summary>
    /// <remarks>This method associates the connection identified by <see cref="Context.ConnectionId"/> with
    /// the specified group. Once added, the connection will receive messages sent to the group.</remarks>
    /// <param name="groupName">The name of the group to join. Cannot be null or empty.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async virtual Task JoinGroup(string groupName)
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
    public async virtual Task LeaveGroup(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }
}
