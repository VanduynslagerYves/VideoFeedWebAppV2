using Microsoft.AspNetCore.SignalR;

namespace CameraFeed.SocketServer.Hubs;

//TODO: inject IForwarderFactory and create a concrete FrontendForwarder
public class CameraWorkerHub(IFrontendForwarder forwarder, ILogger<CameraWorkerHub> logger) : HubBase(logger)
{
    private readonly IFrontendForwarder _forwarder = forwarder;

    public async Task SendMessage(byte[] message, string groupName)
    {
        await _forwarder.ApplyAsync(message, groupName, "ReceiveForwardedMessage");
    }

    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        var apiKey = httpContext?.Request.Headers["X-API-KEY"].FirstOrDefault();
        
        if(string.IsNullOrEmpty(apiKey) || apiKey != "e4b7c1f2-8a3d-4e6b-9c2a-7f5d1b8e3c4a")
        {
            Context.Abort();
            return;
        }

        await base.OnConnectedAsync();
    }
}
