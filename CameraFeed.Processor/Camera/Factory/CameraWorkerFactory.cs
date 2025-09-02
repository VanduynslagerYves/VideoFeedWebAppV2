using CameraFeed.Processor.Services.gRPC;
using Emgu.CV;
using Microsoft.AspNetCore.SignalR;

namespace CameraFeed.Processor.Camera.Factory;

public interface ICameraWorkerFactory
{
    Task<ICameraWorker> CreateCameraWorkerAsync(WorkerOptions options);
}

public class CameraWorkerFactory(IVideoCaptureFactory videoCaptureFactory, IBackgroundSubtractorFactory backgroundSubtractorFactory, IObjectDetectionGrpcClient objectDetectionClient, ILogger<CameraWorker> logger, IHubContext<CameraHub> hubContext) : ICameraWorkerFactory
{
    public Task<ICameraWorker> CreateCameraWorkerAsync(WorkerOptions options)
    {
        var worker = new CameraWorker(options, videoCaptureFactory, backgroundSubtractorFactory, objectDetectionClient, logger, hubContext);
        return Task.FromResult<ICameraWorker>(worker);
    }
}

public interface IBackgroundSubtractorFactory
{
    Task<BackgroundSubtractorMOG2> CreateAsync();
}

public class BackgroundSubtractorFactory : IBackgroundSubtractorFactory
{
    public Task<BackgroundSubtractorMOG2> CreateAsync()
    {
        var subractor = new BackgroundSubtractorMOG2(history: 500, shadowDetection: false);
        return Task.FromResult(subractor);
    }
}
