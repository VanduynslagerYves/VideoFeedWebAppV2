using Microsoft.AspNetCore.SignalR;

namespace CameraFeed.SocketServer.Hubs;

/// <summary>
/// Provides functionality to forward messages to a specific group of clients connected to a SignalR hub.
/// </summary>
/// <remarks>This class is designed to work with a SignalR hub context to send messages to a group of clients.
/// Before calling <see cref="Forward"/>, the hub context must be set using <see cref="SetHubContext"/>.</remarks>
public class MessageForwarder
{
    private IHubContext<FrontendClientHub>? _forwarderHubContext;

    public void SetHubContext(IHubContext<FrontendClientHub> context)
    {
        _forwarderHubContext = context;
    }

    public async Task Forward(byte[] message, string groupName)
    {
        if (_forwarderHubContext != null)
            await _forwarderHubContext.Clients.Group(groupName).SendAsync("ReceiveForwardedMessage", message);
    }
}
