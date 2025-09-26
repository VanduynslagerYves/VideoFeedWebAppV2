using Microsoft.AspNetCore.SignalR;

namespace CameraFeed.SocketServer.Hubs;

public interface IFrontendForwarder
{
    Task ApplyAsync(byte[] message, string groupName, string method);
}

public class FrontendForwarder(IHubContext<FrontendClientHub> context) : IFrontendForwarder
{
    private readonly IHubContext<FrontendClientHub> _forwarderHubContext = context;

    public async Task ApplyAsync(byte[] message, string groupName, string method)
    {
        await _forwarderHubContext.Clients.Group(groupName).SendAsync(method, message);
    }
}

public interface IBackendForwarder
{
    Task ApplyAsync(string groupName, string method);
}

public class BackendForwarder(IHubContext<CameraWorkerHub> context) : IBackendForwarder
{
    private readonly IHubContext<CameraWorkerHub> _backendHubContext = context;

    public async Task ApplyAsync(string groupName, string method)
    {
        await _backendHubContext.Clients.Group(groupName).SendAsync(method);
    }
}
