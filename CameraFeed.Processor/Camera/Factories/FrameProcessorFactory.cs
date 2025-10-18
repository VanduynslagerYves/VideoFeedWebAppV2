using CameraFeed.Processor.Clients.gRPC;

namespace CameraFeed.Processor.Camera.Factories;

public interface IFrameProcessorFactory
{
    Task<IFrameProcessor> CreateAsync(WorkerProperties options, BackgroundSubtractorType subtractorType = BackgroundSubtractorType.MOG2);
}

public class FrameProcessorFactory(IVideoCaptureFactory videoCaptureFactory, IBackgroundSubtractorFactory backgroundSubtractorFactory, IObjectDetectionGrpcClient objectDetectionClient, ILogger<FrameProcessor> logger) : IFrameProcessorFactory
{
    public async Task<IFrameProcessor> CreateAsync(WorkerProperties options, BackgroundSubtractorType subtractorType = BackgroundSubtractorType.MOG2)
    {
        var videoCapture = await videoCaptureFactory.CreateAsync(options);
        var subtractor = backgroundSubtractorFactory.Create(subtractorType);

        return new FrameProcessor(videoCapture, subtractor, objectDetectionClient, options, logger);
    }
}
