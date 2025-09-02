using CameraFeed.Processor.Camera.Worker;
using CameraFeed.Processor.Camera.Worker;
using CameraFeed.Processor.Services.gRPC;
using Microsoft.AspNetCore.SignalR;

namespace CameraFeed.Processor.Camera.Builder;

public class CameraWorkerBuilder(
    ILogger<CameraWorker> logger,
    IHubContext<CameraHub> hubContext,
    IVideoCaptureFactory videoCaptureFactory,
    IBackgroundSubtractorFactory backgroundSubtractorFactory,
    IObjectDetectionGrpcClient objectDetectionClient)
{
    private int _cameraId;
    private bool _useMotionDetection;
    private CameraResolution _resolution = new() { Width = 1920, Height = 1080 };
    private int _framerate = 30;

    public CameraWorkerBuilder WithCameraId(int cameraId)
    {
        _cameraId = cameraId;
        return this;
    }

    public CameraWorkerBuilder WithResolution(int width, int height)
    {
        _resolution = new CameraResolution { Width = width, Height = height };
        return this;
    }

    public CameraWorkerBuilder WithFramerate(int framerate)
    {
        _framerate = framerate;
        return this;
    }

    public CameraWorkerBuilder UseMotionDetection(bool value = true)
    {
        _useMotionDetection = value;
        return this;
    }

    public Task<ICameraWorker> Build()
    {
        var options = new WorkerOptions
        {
            CameraId = _cameraId,
            Mode = InferenceMode.MotionBased,
            CameraOptions = new CameraOptions
            {
                Resolution = _resolution,
                Framerate = _framerate
            }
        };
        
        var worker = new CameraWorker(
            options,
            videoCaptureFactory,
            backgroundSubtractorFactory,
            objectDetectionClient,
            logger,
            hubContext);

        return Task.FromResult<ICameraWorker>(worker);
    }
}