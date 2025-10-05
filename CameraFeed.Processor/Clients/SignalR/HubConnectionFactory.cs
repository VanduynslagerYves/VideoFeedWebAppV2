using Microsoft.AspNetCore.SignalR.Client;

namespace CameraFeed.Processor.Clients.SignalR;

public interface IHubConnectionFactory
{
    HubConnection CreateLocalConnection(Action? onStreamingEnabled = null, Action? onStreamingDisabled = null);
    HubConnection CreateRemoteConnection(Action? onStreamingEnabled = null, Action? onStreamingDisabled = null);
}

public class HubConnectionFactory : IHubConnectionFactory
{
    protected readonly string _apiKey = "e4b7c1f2-8a3d-4e6b-9c2a-7f5d1b8e3c4a"; //TODO: Generate in local webinterface through SocketServer endpoint
    protected readonly string _remoteHubUrl = "https://localhost:7000/workerhub"; //TODO: get from config
    protected readonly string _localHubUrl = "https://localhost:7244/workerhub"; //TODO: get from config

    public HubConnection CreateLocalConnection(Action? onStreamingEnabled = null, Action? onStreamingDisabled = null)
    {
        var connection = new HubConnectionBuilder()
            .WithUrl(_localHubUrl, options => { options.Headers.Add("X-API-KEY", _apiKey); })
            .Build();

        if (onStreamingEnabled != null) connection.On("NotifyStreamingEnabled", onStreamingEnabled);
        if (onStreamingDisabled != null) connection.On("NotifyStreamingDisabled", onStreamingDisabled);

        return connection;
    }

    public HubConnection CreateRemoteConnection(Action? onStreamingEnabled = null, Action? onStreamingDisabled = null)
    {
        var connection = new HubConnectionBuilder()
             .WithUrl(_remoteHubUrl, options => { options.Headers.Add("X-API-KEY", _apiKey); })
             .Build();

        if (onStreamingEnabled != null) connection.On("NotifyStreamingEnabled", onStreamingEnabled);
        if (onStreamingDisabled != null) connection.On("NotifyStreamingDisabled", onStreamingDisabled);

        return connection;
    }
}
