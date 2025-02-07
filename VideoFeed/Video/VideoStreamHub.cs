using Microsoft.AspNetCore.SignalR;

namespace VideoFeed.Video;

public class VideoStreamHub : Hub
{
    //public async Task BroadCastVideo(byte[] videoData)
    //{
    //    await Clients.All.SendAsync("ReceiveVideo", videoData);
    //}
}
