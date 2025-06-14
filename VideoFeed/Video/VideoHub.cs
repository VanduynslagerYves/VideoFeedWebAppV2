using Microsoft.AspNetCore.SignalR;

namespace VideoFeed.Video;

public class VideoHub : Hub
{
    public override Task OnConnectedAsync()
    {
        Console.WriteLine("Client connected: " + Context.ConnectionId);
        return base.OnConnectedAsync();
    }
}
