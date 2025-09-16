using CameraFeed.Processor.Services.gRPC;
using Microsoft.AspNetCore.SignalR;

namespace CameraFeed.Processor.Camera.Worker;

public interface ICameraWorkerFactory
{
    Task<ICameraWorker> CreateAsync(WorkerOptions options);
}

public class CameraWorkerFactory(IVideoCaptureFactory videoCaptureFactory, IBackgroundSubtractorFactory backgroundSubtractorFactory, IObjectDetectionGrpcClient objectDetectionClient, ILogger<CameraWorker> logger, IHubContext<CameraHub> hubContext) : ICameraWorkerFactory
{
    public Task<ICameraWorker> CreateAsync(WorkerOptions options)
    {
        var worker = new CameraWorker(options, videoCaptureFactory, backgroundSubtractorFactory, objectDetectionClient, logger, hubContext);
        return Task.FromResult<ICameraWorker>(worker);
    }
}