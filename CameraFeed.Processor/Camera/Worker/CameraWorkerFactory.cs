using CameraFeed.Processor.Clients.gRPC;

namespace CameraFeed.Processor.Camera.Worker;

public interface ICameraWorkerFactory
{
    Task<ICameraWorker> CreateAsync(WorkerProperties options);
}

public class CameraWorkerFactory(IVideoCaptureFactory videoCaptureFactory, IBackgroundSubtractorFactory backgroundSubtractorFactory, IObjectDetectionGrpcClient objectDetectionClient, ILogger<CameraWorker> logger) : ICameraWorkerFactory
{
    public Task<ICameraWorker> CreateAsync(WorkerProperties options)
    {
        var worker = new CameraWorker(options, videoCaptureFactory, backgroundSubtractorFactory, objectDetectionClient, logger);
        return Task.FromResult<ICameraWorker>(worker);
    }
}