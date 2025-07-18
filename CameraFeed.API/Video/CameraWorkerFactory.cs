using Microsoft.AspNetCore.SignalR;

namespace CameraAPI.Video;

public interface ICameraWorkerFactory
{
    public ICameraWorker CreateCameraWorker(int cameraId);
}

public class CameraWorkerFactory(ILogger<CameraWorker> logger, IHubContext<CameraHub> hubContext) : ICameraWorkerFactory
{
    private readonly IHubContext<CameraHub> _hubContext = hubContext;
    private readonly ILogger<CameraWorker> _logger = logger;

    public ICameraWorker CreateCameraWorker(int cameraId)
    {
        return new CameraWorker(cameraId, _logger, _hubContext);
    }
}
