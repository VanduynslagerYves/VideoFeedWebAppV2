using Microsoft.AspNetCore.SignalR.Client;

namespace CameraFeed.Processor.Camera.Worker;

public interface IHubConnectionFactory
{
    HubConnection CreateConnection(string url, string apiKey);
}

public class HubConnectionFactory : IHubConnectionFactory
{
    public HubConnection CreateConnection(string url, string apiKey)
    {
        var connection = new HubConnectionBuilder()
            .WithUrl(url, options =>
            {
                options.Headers.Add("X-API-KEY", apiKey);
            })
            .Build();

        return connection;
    }
}
