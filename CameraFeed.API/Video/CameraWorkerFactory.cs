using Microsoft.AspNetCore.SignalR;

namespace CameraFeed.API.Video;
public interface ICameraWorkerFactory
{
    Task<ICameraWorker> CreateCameraWorkerAsync(CameraWorkerOptions options);
}

public class CameraWorkerFactory(ILogger<CameraWorker> logger, IHubContext<CameraHub> hubContext) : ICameraWorkerFactory
{
    private readonly IHubContext<CameraHub> _hubContext = hubContext;
    private readonly ILogger<CameraWorker> _logger = logger;

    public async Task<ICameraWorker> CreateCameraWorkerAsync(CameraWorkerOptions options)
    {
        return await Task.FromResult(new CameraWorker(options, _logger, _hubContext));
    }
}
