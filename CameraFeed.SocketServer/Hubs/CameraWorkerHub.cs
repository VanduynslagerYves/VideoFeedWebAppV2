using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CameraFeed.SocketServer.Hubs;

/// <summary>
/// Represents a SignalR hub that receives messages and forwards them to a specified group.
/// It receives image bytes from camera workers and forwards them to front-end clients"/>
/// </summary>
/// <remarks>This hub is designed to handle incoming messages and forward them to a specific group using the
/// provided <see cref="MessageForwarder"/> instance.</remarks>
/// <param name="forwarder"></param>
/// <param name="logger"></param>
public class CameraWorkerHub(IHubContext<CameraWorkerHub> context, ILogger<CameraWorkerHub> logger) : HubBase(logger)
{
    private readonly IHubContext<CameraWorkerHub> _context = context;
    //private readonly IMessageForwarder _forwarder = forwarder;

    public async Task SendMessage(byte[] message, string groupName)
    {
        await _context.Clients.Group(groupName).SendAsync("ReceiveForwardedMessage", message);
    }

    public async Task StartStreaming(string cameraName)
    {
        await _context.Clients.Group(cameraName).SendAsync("NotifyStreamingEnabled");
    }

    public async Task StopStreaming(string cameraName)
    {
        if (_context != null)
            await _context.Clients.Group(cameraName).SendAsync("NotifyStreamingDisabled");
    }
}
