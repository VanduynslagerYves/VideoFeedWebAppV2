using Microsoft.AspNetCore.SignalR;

namespace CameraFeed.SocketServer.Hubs;

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
        
        if(string.IsNullOrEmpty(apiKey) || apiKey != "123456789")
        {
            Context.Abort();
            return;
        }

        await base.OnConnectedAsync();
    }
}
