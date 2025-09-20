namespace CameraFeed.SocketServer.Hubs;

/// <summary>
/// Represents a SignalR hub that receives messages and forwards them to a specified group.
/// It receives image bytes from camera workers and forwards them to front-end clients"/>
/// </summary>
/// <remarks>This hub is designed to handle incoming messages and forward them to a specific group using the
/// provided <see cref="MessageForwarder"/> instance.</remarks>
/// <param name="forwarder"></param>
/// <param name="logger"></param>
public class CameraWorkerHub(IMessageForwarder forwarder, ILogger<CameraWorkerHub> logger) : HubBase(logger)
{
    private readonly IMessageForwarder _forwarder = forwarder;

    public async Task ReceiveMessage(byte[] message, string groupName)
    {
        await _forwarder.Apply(message, groupName);
    }
}
