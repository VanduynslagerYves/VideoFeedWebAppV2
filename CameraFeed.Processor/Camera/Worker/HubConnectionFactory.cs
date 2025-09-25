using Microsoft.AspNetCore.SignalR.Client;

namespace CameraFeed.Processor.Camera.Worker;

public interface IHubConnectionFactory
{
    HubConnection CreateLocalConnection(string url);
    HubConnection CreateRemoteConnection(string url);
}

public class HubConnectionFactory : IHubConnectionFactory
{
    public HubConnection CreateLocalConnection(string url)
    {
        var connection = new HubConnectionBuilder()
            .WithUrl(url, options =>
            {
                //options.Headers.Add("Authorization", "ApiKey abcdefghijklmnop");
            })
            .Build();

        return connection;
    }

    public HubConnection CreateRemoteConnection(string url)
    {
        var connection = new HubConnectionBuilder()
            .WithUrl(url)
            .Build();

        return connection;
    }
}
