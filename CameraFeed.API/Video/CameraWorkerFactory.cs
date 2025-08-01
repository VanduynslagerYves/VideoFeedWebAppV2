using CameraFeed.API.ApiClients;
using Microsoft.AspNetCore.SignalR;

namespace CameraFeed.API.Video;
public interface ICameraWorkerFactory
{
    Task<ICameraWorker> CreateCameraWorkerAsync(CameraWorkerOptions options);
}

public class CameraWorkerFactory(ILogger<CameraWorker> logger, IHubContext<CameraHub> hubContext, IHumanDetectionApiClient humanDetectionApiClient) : ICameraWorkerFactory
{
    private readonly IHumanDetectionApiClient _humanDetectionApiClient = humanDetectionApiClient;
    private readonly IHubContext<CameraHub> _hubContext = hubContext;
    private readonly ILogger<CameraWorker> _logger = logger;

    public async Task<ICameraWorker> CreateCameraWorkerAsync(CameraWorkerOptions options)
    {
        return await Task.FromResult(new CameraWorker(options, _logger, _humanDetectionApiClient, _hubContext));
    }
}
