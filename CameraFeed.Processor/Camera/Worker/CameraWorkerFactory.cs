using CameraFeed.Processor.Clients;
using CameraFeed.Processor.Clients.gRPC;
using Microsoft.AspNetCore.SignalR;

namespace CameraFeed.Processor.Camera.Worker;

public interface ICameraWorkerFactory
{
    Task<ICameraWorker> CreateAsync(WorkerProperties options);
}

public class CameraWorkerFactory(IVideoCaptureFactory videoCaptureFactory, IBackgroundSubtractorFactory backgroundSubtractorFactory, IObjectDetectionGrpcClient objectDetectionClient, ILogger<CameraWorker> logger, IHubContext<CameraHub> hubContext) : ICameraWorkerFactory
{
    public Task<ICameraWorker> CreateAsync(WorkerProperties options)
    {
        var worker = new CameraWorker(options, videoCaptureFactory, backgroundSubtractorFactory, objectDetectionClient, logger, hubContext);
        return Task.FromResult<ICameraWorker>(worker);
    }
}