using CameraFeed.API.Services;
using Emgu.CV;
using Microsoft.AspNetCore.SignalR;

namespace CameraFeed.API.Video;
public interface ICameraWorkerFactory
{
    Task<ICameraWorker> CreateCameraWorkerAsync(CameraWorkerOptions options);
}

public class CameraWorkerFactory(ILogger<CameraWorker> logger, IHubContext<CameraHub> hubContext, IVideoCaptureFactory videoCaptureFactory, IBackgroundSubtractorFactory backgroundSubtractorFactory, IObjectDetectionApiClient humanDetectionApiClient) : ICameraWorkerFactory
{
    public async Task<ICameraWorker> CreateCameraWorkerAsync(CameraWorkerOptions options)
    {
        return await Task.FromResult(new CameraWorker(options, videoCaptureFactory, backgroundSubtractorFactory, humanDetectionApiClient, logger, hubContext));
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
