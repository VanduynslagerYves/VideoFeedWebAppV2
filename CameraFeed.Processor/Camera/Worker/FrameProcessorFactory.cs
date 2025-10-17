using CameraFeed.Processor.Clients.gRPC;

namespace CameraFeed.Processor.Camera.Worker;

public interface IFrameProcessorFactory
{
    IFrameProcessor Create(WorkerProperties options);
}

public class FrameProcessorFactory(IVideoCaptureFactory videoCaptureFactory, IBackgroundSubtractorFactory backgroundSubtractorFactory, IObjectDetectionGrpcClient objectDetectionClient, ILogger<FrameProcessor> logger) : IFrameProcessorFactory
{
    public IFrameProcessor Create(WorkerProperties options)
    {
        return new FrameProcessor(videoCaptureFactory, backgroundSubtractorFactory, objectDetectionClient, options, logger);
    }
}
