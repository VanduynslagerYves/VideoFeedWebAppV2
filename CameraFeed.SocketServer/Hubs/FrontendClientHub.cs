using Microsoft.AspNetCore.Authorization;

namespace CameraFeed.SocketServer.Hubs;

//TODO: inject IForwarderFactory and create a concrete BackendForwarderForwarder
[Authorize]
public class FrontendClientHub(IBackendForwarder forwarder, ILogger<FrontendClientHub> logger) : HubBase(logger)
{
    private readonly IBackendForwarder _forwarder = forwarder;

    public async Task StartStreaming(string cameraName)
    {
        await _forwarder.ApplyAsync(cameraName, "NotifyStreamingEnabled");
    }

    public async Task StopStreaming(string cameraName)
    {
        await _forwarder.ApplyAsync(cameraName, "NotifyStreamingDisabled");
    }
}
