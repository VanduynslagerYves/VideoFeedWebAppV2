using Microsoft.AspNetCore.SignalR;

namespace VideoFeed.Video;

public interface ICameraWorkerFactory
{
    public ICameraWorker CreateCameraWorker(int cameraId);
}

public class CameraWorkerFactory : ICameraWorkerFactory
{
    private readonly IHubContext<VideoHub> _hubContext;
    private readonly ILogger<CameraWorker> _logger;
    private readonly IServiceProvider _serviceProvider;

    public CameraWorkerFactory(ILogger<CameraWorker> logger, IHubContext<VideoHub> hubContext, IServiceProvider serviceProvider)
    {
        _hubContext = hubContext;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public ICameraWorker CreateCameraWorker(int cameraId)
    {
        var logger = _serviceProvider.GetRequiredService<ILogger<CameraWorker>>();
        var cts = new CancellationTokenSource();

        return new CameraWorker(cameraId, _logger, _hubContext);
    }
}
