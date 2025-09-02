using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Microsoft.AspNetCore.SignalR;
using CameraFeed.Processor.Services.gRPC;
using CameraFeed.Processor.Camera.Worker;

namespace CameraFeed.Processor.Camera.Worker;

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
            using var subtractor = await _backgroundSubtractorFactory.CreateAsync(type: BackgroundSubtractorType.MOG2);
            using var foregroundMask = new Mat();

            while (!token.IsCancellationRequested)
            {
                using var capturedFrame = capture!.QueryFrame();
                if (capturedFrame == null || capturedFrame.IsEmpty) continue;

                var imageByteArray = ConvertFrameToByteArray(capturedFrame);

                if (ShouldRunInference(capturedFrame, foregroundMask, subtractor))
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

    private bool ShouldRunInference(Mat capturedFrame, Mat fgMask, IBackgroundSubtractorAdapter subtractor)
    {
        switch(_options.Mode)
        {
            case InferenceMode.Continuous:
                return true;
            case InferenceMode.MotionBased:
                return MotionDetected(capturedFrame, fgMask, subtractor); // Handled separately in the main loop
            default:
                return false;
        }
    }

    public virtual async Task<byte[]> RunInference(byte[] frameData, CancellationToken cancellationToken = default)
    {
        // Call the gRPC object detection
        byte[] processedFrame = await _objectDetectionClient.DetectObjectsAsync(frameData, cancellationToken);
        return processedFrame;
    }

    protected virtual bool MotionDetected(Mat frame, Mat foregroundMask, IBackgroundSubtractorAdapter subtractor)
    {
        // Downscale frame for faster processing
        var downscaleFactor = 16; // Downscale by a factor of 16
        int downscaledWidth = frame.Width / downscaleFactor;
        int downscaledHeight = frame.Height / downscaleFactor;

        using var downscaledFrame = new Mat();
        CvInvoke.Resize(frame, downscaledFrame, new System.Drawing.Size(downscaledWidth, downscaledHeight), interpolation: Inter.Linear);

        subtractor.Apply(downscaledFrame, foregroundMask); // foregroundMask now contains white pixels where motion is detected
        int motionPixels = CvInvoke.CountNonZero(foregroundMask);

        int downscaledArea = downscaledWidth * downscaledHeight;
        double motionRatio = 0.03; // 3% of the area
        int threshold = (int)(downscaledArea * motionRatio);

        if (motionPixels > threshold)
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
    public required InferenceMode Mode { get; set; }
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

public enum InferenceMode
{
    MotionBased,
    Continuous
}