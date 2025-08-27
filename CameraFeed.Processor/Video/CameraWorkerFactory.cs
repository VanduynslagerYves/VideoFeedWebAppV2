using CameraFeed.Processor.Services.gRPC;
using Emgu.CV;
using Microsoft.AspNetCore.SignalR;

namespace CameraFeed.Processor.Video;
public interface ICameraWorkerFactory
{
    Task<ICameraWorker> CreateCameraWorkerAsync(StartWorkerOptions options);
}

public class CameraWorkerFactory(ILogger<CameraWorker> logger, IHubContext<CameraHub> hubContext, IVideoCaptureFactory videoCaptureFactory, IBackgroundSubtractorFactory backgroundSubtractorFactory, IObjectDetectionGrpcClient objectDetectionClient) : ICameraWorkerFactory
{
    public async Task<ICameraWorker> CreateCameraWorkerAsync(StartWorkerOptions options)
    {
        return await Task.FromResult(new CameraWorker(options, videoCaptureFactory, backgroundSubtractorFactory, objectDetectionClient, logger, hubContext));
    }
}

public interface IVideoCaptureFactory
{
    VideoCapture Create(int cameraId);
}

public class VideoCaptureFactory : IVideoCaptureFactory
{
    public VideoCapture Create(int cameraId)
    {
        return new VideoCapture(cameraId);
    }
}

public interface IBackgroundSubtractorFactory
{
    BackgroundSubtractorMOG2 Create();
}

public class BackgroundSubtractorFactory : IBackgroundSubtractorFactory
{
    public BackgroundSubtractorMOG2 Create()
    {
        return new BackgroundSubtractorMOG2();
    }
}
