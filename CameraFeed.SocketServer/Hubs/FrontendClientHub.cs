using Microsoft.AspNetCore.Authorization;

namespace CameraFeed.SocketServer.Hubs;

[Authorize]
public class FrontendClientHub(IBackendForwarder forwarder, ILogger<FrontendClientHub> logger) : HubBase(logger)
{
    private readonly IBackendForwarder _forwarder = forwarder;

    //[Authorize]
    public async Task StartStreaming(string cameraName)
    {
        await _forwarder.ApplyAsync(cameraName, "NotifyStreamingEnabled");
    }

    //[Authorize]
    public async Task StopStreaming(string cameraName)
    {
        await _forwarder.ApplyAsync(cameraName, "NotifyStreamingDisabled");
    }
}
