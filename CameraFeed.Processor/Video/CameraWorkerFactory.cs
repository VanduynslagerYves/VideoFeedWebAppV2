using CameraFeed.Processor.Services.gRPC;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Microsoft.AspNetCore.SignalR;

namespace CameraFeed.Processor.Video;

public interface ICameraWorkerFactory
{
    Task<ICameraWorker> CreateCameraWorkerAsync(WorkerOptions options);
}

public class CameraWorkerFactory(ILogger<CameraWorker> logger, IHubContext<CameraHub> hubContext, IVideoCaptureFactory videoCaptureFactory, IBackgroundSubtractorFactory backgroundSubtractorFactory, IObjectDetectionGrpcClient objectDetectionClient) : ICameraWorkerFactory
{
    public async Task<ICameraWorker> CreateCameraWorkerAsync(WorkerOptions options)
    {
        var capture = await videoCaptureFactory.CreateAsync(options);
        return new CameraWorker(capture, options, backgroundSubtractorFactory, objectDetectionClient, logger, hubContext);
    }
}

public interface IVideoCaptureFactory
{
    Task<VideoCapture> CreateAsync(WorkerOptions options);
}

public class VideoCaptureFactory : IVideoCaptureFactory
{
    public Task<VideoCapture> CreateAsync(WorkerOptions options)
    {
        return Task.Run(() =>
        {
            var videoCapture = new VideoCapture(options.CameraId);

            if (videoCapture == null || !videoCapture.IsOpened)
                throw new InvalidOperationException($"Camera {options.CameraId} could not be initialized.");

            videoCapture.Set(CapProp.FrameWidth, options.CameraOptions.Resolution.Width);
            videoCapture.Set(CapProp.FrameHeight, options.CameraOptions.Resolution.Height);
            videoCapture.Set(CapProp.Fps, options.CameraOptions.Framerate);
            videoCapture.Set(CapProp.FourCC, VideoWriter.Fourcc('M', 'J', 'P', 'G'));

            return videoCapture;
        });
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
