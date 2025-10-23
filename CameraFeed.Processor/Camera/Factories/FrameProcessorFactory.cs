using CameraFeed.Processor.Clients.gRPC;

namespace CameraFeed.Processor.Camera.Factories;

public interface IFrameProcessorFactory
{
    Task<IFrameProcessor> CreateAsync(WorkerProperties options, BackgroundSubtractorType subtractorType = BackgroundSubtractorType.MOG2);
}

public class FrameProcessorFactory(IBackgroundSubtractorFactory backgroundSubtractorFactory, IObjectDetectionGrpcClient objectDetectionClient, ILogger<FrameProcessor> logger) : IFrameProcessorFactory
{
    public async Task<IFrameProcessor> CreateAsync(WorkerProperties options, BackgroundSubtractorType subtractorType = BackgroundSubtractorType.MOG2)
    {
        var videoCaptureBuilder = new VideoCaptureBuilder(cameraId: options.CameraOptions.Id)
            .WithResolution(width: options.CameraOptions.Resolution.Width, height: options.CameraOptions.Resolution.Height)
            .WithFramerate(options.CameraOptions.Framerate);
            //.WithFourCC("MJPG");

        var videoCapture = await videoCaptureBuilder.BuildAsync();
        var subtractor = backgroundSubtractorFactory.Create(subtractorType);

        return new FrameProcessor(videoCapture, subtractor, objectDetectionClient, options, logger);
    }
}
