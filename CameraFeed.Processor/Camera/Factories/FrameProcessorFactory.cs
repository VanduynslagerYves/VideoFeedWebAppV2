using CameraFeed.Processor.Clients.gRPC;

namespace CameraFeed.Processor.Camera.Factories;

public interface IFrameProcessorFactory
{
    Task<IFrameProcessor> CreateAsync(WorkerProperties options);
}

public class FrameProcessorFactory(IVideoCaptureFactory videoCaptureFactory, IBackgroundSubtractorFactory backgroundSubtractorFactory, IObjectDetectionGrpcClient objectDetectionClient, ILogger<FrameProcessor> logger) : IFrameProcessorFactory
{
    public async Task<IFrameProcessor> CreateAsync(WorkerProperties options)
    {
        var frameProcessor = new FrameProcessor(videoCaptureFactory, backgroundSubtractorFactory, objectDetectionClient, options, logger);
        await frameProcessor.InitializeVideoCaptureAsync();
        return frameProcessor;
    }
}
