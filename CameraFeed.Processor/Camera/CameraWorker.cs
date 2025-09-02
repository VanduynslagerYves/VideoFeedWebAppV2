using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Microsoft.AspNetCore.SignalR;
using CameraFeed.Processor.Services.gRPC;
using CameraFeed.Processor.Camera.Factory;

namespace CameraFeed.Processor.Camera;

public interface ICameraWorker
{
    Task RunAsync(CancellationToken token);
    int CameraId { get; }
}

public abstract class CameraWorkerBase(WorkerOptions options, IVideoCaptureFactory videoCaptureFactory, IBackgroundSubtractorFactory backgroundSubtractorFactory, IObjectDetectionGrpcClient objectDetectioniClient,
    IHubContext<CameraHub> hubContext) : ICameraWorker
{
    protected readonly IObjectDetectionGrpcClient _objectDetectionClient = objectDetectioniClient;
    protected readonly IHubContext<CameraHub> _hubContext = hubContext;
    protected readonly IBackgroundSubtractorFactory _backgroundSubtractorFactory = backgroundSubtractorFactory;
    protected readonly IVideoCaptureFactory _videoCaptureFactory = videoCaptureFactory;
    protected readonly WorkerOptions _options = options;

    protected string NotifyImageGroup => $"camera_{CameraId}";

    public int CameraId { get; } = options.CameraId;

    public abstract Task RunAsync(CancellationToken token);
}

public class CameraWorker(WorkerOptions options, IVideoCaptureFactory videoCaptureFactory, IBackgroundSubtractorFactory backgroundSubtractorFactory, IObjectDetectionGrpcClient objectDetectionClient,
    ILogger<CameraWorker> logger, IHubContext<CameraHub> hubContext) : CameraWorkerBase(options, videoCaptureFactory, backgroundSubtractorFactory, objectDetectionClient, hubContext)
{
    private readonly ILogger<CameraWorker> _logger = logger;

    public override async Task RunAsync(CancellationToken token)
    {
        try
        {
            using var capture = await _videoCaptureFactory.CreateAsync(_options);
            using var subtractor = await _backgroundSubtractorFactory.CreateAsync();
            using var fgMask = new Mat();

            while (!token.IsCancellationRequested)
            {
                using var capturedFrame = capture!.QueryFrame();
                if (capturedFrame == null || capturedFrame.IsEmpty) continue;

                var imageByteArray = ConvertFrameToByteArray(capturedFrame);

                if (_options.UseContinuousInference || (_options.UseMotionDetection && MotionDetected(capturedFrame, fgMask, subtractor)))
                {
                    imageByteArray = await RunInference(imageByteArray, token);
                }

                await SendFrameToClientsAsync(imageByteArray, token);
            }
        }
        catch(OperationCanceledException)
        {
            _logger.LogInformation("CameraWorker for camera {cameraId} was cancelled.", CameraId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred in CameraWorker for camera {cameraId}: {message}", CameraId, ex.Message);
        }
        finally
        {
            _logger.LogInformation("CameraWorker for camera {cameraId} has stopped.", CameraId);
        }
    }

    public virtual async Task<byte[]> RunInference(byte[] frameData, CancellationToken cancellationToken = default)
    {
        // Call the gRPC object detection
        byte[] processedFrame = await _objectDetectionClient.DetectObjectsAsync(frameData, cancellationToken);
        return processedFrame;
    }

    // This is expensive wtf
    protected virtual bool MotionDetected(Mat frame, Mat fgMask, BackgroundSubtractorMOG2 subtractor)
    {
        // Downscale frame for faster processing
        var downscaleFactor = 16; // Downscale by a factor of 16

        using var smallFrame = new Mat();
        CvInvoke.Resize(frame, smallFrame, new System.Drawing.Size(frame.Width / downscaleFactor, frame.Height / downscaleFactor), interpolation: Inter.Linear);

        subtractor.Apply(smallFrame, fgMask);

        int motionPixels = CvInvoke.CountNonZero(fgMask);
        // Adjust threshold for smaller frame
        if (motionPixels > 4000 / (downscaleFactor ^ 2)) // 1/256th of original area
        {
            _logger.LogInformation("Motion detected in frame with {motionPixels} pixels at {timeStamp}", motionPixels, DateTime.Now);
            return true;
        }
        return false;
    }

    protected virtual byte[] ConvertFrameToByteArray(Mat frame, int quality = 70)
    {
        // Encode the Mat to JPEG directly into a byte array
        var imageBytes = frame.ToImage<Bgr, byte>().ToJpegData(quality);
        return imageBytes;
    }

    protected virtual async Task SendFrameToClientsAsync(byte[] imageByteArray, CancellationToken token)
    {
        await _hubContext.Clients.Group(NotifyImageGroup).SendAsync("ReceiveImgBytes", imageByteArray, token);
    }
}

public class WorkerOptions
{
    public required int CameraId { get; set; }
    public required bool UseContinuousInference { get; set; } = false;
    public required bool UseMotionDetection { get; set; } = false;
    public required CameraOptions CameraOptions { get; set; }
}

public class CameraOptions
{
    public required CameraResolution Resolution { get; set; }
    public required int Framerate { get; set; }
}

public class CameraResolution
{
    public required int Width;
    public required int Height;
}