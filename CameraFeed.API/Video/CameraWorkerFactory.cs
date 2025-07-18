using Microsoft.AspNetCore.SignalR;

namespace CameraFeed.API.Video;
public interface ICameraWorkerFactory
{
    Task<ICameraWorker> CreateCameraWorkerAsync(int cameraId);
}

public class CameraWorkerFactory(ILogger<CameraWorker> logger, IHubContext<CameraHub> hubContext) : ICameraWorkerFactory
{
    private readonly IHubContext<CameraHub> _hubContext = hubContext;
    private readonly ILogger<CameraWorker> _logger = logger;

    public async Task<ICameraWorker> CreateCameraWorkerAsync(int cameraId)
    {
        return await Task.FromResult(new CameraWorker(cameraId, _logger, _hubContext));
    }
}
