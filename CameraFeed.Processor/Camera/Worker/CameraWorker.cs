using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Microsoft.AspNetCore.SignalR;
using CameraFeed.Processor.Services.gRPC;
using CameraFeed.Processor.Extensions;

namespace CameraFeed.Processor.Camera.Worker;

public interface ICameraWorker
{
    Task RunAsync(CancellationToken token);
    int CamId { get; }
    string CamName { get; }
    int CamWidth { get; }
    int CamHeight { get; }
}

public abstract class CameraWorkerBase : ICameraWorker
{
    protected readonly IObjectDetectionGrpcClient _objectDetectionClient;
    protected readonly IHubContext<CameraHub> _hubContext;
    protected readonly IBackgroundSubtractorFactory _backgroundSubtractorFactory;
    protected readonly IVideoCaptureFactory _videoCaptureFactory;
    protected readonly WorkerOptions _options;

    protected readonly int _frameSkip = 3;
    protected readonly int _downscaledWidth;
    protected readonly int _downscaledHeight;
    protected readonly int _motionThreshold;

    protected int _frameCounter = 0;
    protected bool _lastMotionResult = false;
    protected Mat _downscaledFrame = new();

    public CameraWorkerBase(WorkerOptions options, IVideoCaptureFactory videoCaptureFactory, IBackgroundSubtractorFactory backgroundSubtractorFactory, IObjectDetectionGrpcClient objectDetectionClient,
        IHubContext<CameraHub> hubContext)
    {
        _options = options;
        _videoCaptureFactory = videoCaptureFactory;
        _backgroundSubtractorFactory = backgroundSubtractorFactory;
        _objectDetectionClient = objectDetectionClient;
        _hubContext = hubContext;

        CamId = options.CameraOptions.Id;
        CamName = options.CameraOptions.Name;
        CamWidth = options.CameraOptions.Resolution.Width;
        CamHeight = options.CameraOptions.Resolution.Height;

        //Setup motion detection parameters
        _downscaledHeight = options.CameraOptions.Resolution.Height / options.MotionDetectionOptions.DownscaleFactor;
        _downscaledWidth = options.CameraOptions.Resolution.Width / options.MotionDetectionOptions.DownscaleFactor;
        int downscaledArea = _downscaledWidth * _downscaledHeight;
        _motionThreshold = (int)(downscaledArea * options.MotionDetectionOptions.MotionRatio);
    }

    protected string NotifyImageGroup => $"camera_{CamId}";

    public int CamId { get; }
    public string CamName { get; }
    public int CamWidth { get; }
    public int CamHeight { get; }

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
            _logger.LogInformation("CameraWorker for camera {cameraId} was cancelled.", CamId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred in CameraWorker for camera {cameraId}: {message}", CamId, ex.Message);
        }
        finally
        {
            _logger.LogInformation("CameraWorker for camera {cameraId} has stopped.", CamId);
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
        return await _objectDetectionClient.DetectObjectsAsync(frameData, cancellationToken);
    }

    /// <summary>
    /// Determines whether significant motion is detected in the current video frame.
    /// Optimized for CPU usage by downscaling and frame skipping.
    /// </summary>
    /// <param name="frame">The original captured video frame.</param>
    /// <param name="foregroundMask">A Mat to store the resulting foreground mask.</param>
    /// <param name="subtractor">The background subtractor instance.</param>
    /// <returns>True if motion is detected, otherwise false.</returns>
    protected virtual bool MotionDetected(Mat frame, Mat foregroundMask, IBackgroundSubtractorAdapter subtractor)
    {
        if (_frameCounter++ % _frameSkip != 0) return _lastMotionResult; // Only process every n-th frame, by frame skipping, to reduce CPU usage

        // Downscale the frame to reduce the number of pixels to process, improving performance.
        // Uses nearest neighbor interpolation for speed, which is sufficient for motion detection.
        frame.DownscaleTo(
            destination: _downscaledFrame,
            toWidth: _downscaledWidth,
            toHeight: _downscaledHeight,
            interpolationMethod: Inter.Nearest);

        // Alternative: Use pyramid downscaling for large reductions
        // _downscaledFrame = frame.PyramidDownscale(toWidth: _downscaledWidth, toHeight: _downscaledHeight);

        // Apply background subtraction to the downscaled frame.
        // The foregroundMask will contain white pixels where motion is detected.
        subtractor.Apply(_downscaledFrame, foregroundMask);

        // Count the number of non-zero (white) pixels in the mask, representing areas of motion.
        int motionPixels = CvInvoke.CountNonZero(foregroundMask);

        // Determine if the number of motion pixels exceeds the configured threshold.
        bool motionDetected = motionPixels > _motionThreshold;

        // Log only when the motion detection state changes (to reduce log noise).
        if (motionDetected != _lastMotionResult) _logger.LogInformation("Motion {status} with {pixels} pixels at {time}", motionDetected ? "detected" : "stopped", motionPixels, DateTime.Now);

        // Store the result for use in skipped frames and for change detection.
        _lastMotionResult = motionDetected;
        return motionDetected;
    }

    protected virtual byte[] ConvertFrameToByteArray(Mat frame, int quality = 78)
    {
        // Encode the Mat to JPEG directly into a byte array
        return frame.ToImage<Bgr, byte>().ToJpegData(quality);
    }

    protected virtual async Task SendFrameToClientsAsync(byte[] imageByteArray, CancellationToken token)
    {
        await _hubContext.Clients.Group(NotifyImageGroup).SendAsync("ReceiveImgBytes", imageByteArray, token);
    }
}

public class WorkerOptions
{
    public required InferenceMode Mode { get; set; }
    public required CameraProperties CameraOptions { get; set; }
    public required MotionDetectionOptions MotionDetectionOptions { get; set; }
}

public class CameraProperties
{
    public required int Id { get; set; }
    public required string Name { get; set; }
    public required CameraResolution Resolution { get; set; }
    public required int Framerate { get; set; }
}

public class CameraResolution
{
    public required int Width { get; set; }
    public required int Height { get; set; }
}

public class MotionDetectionOptions
{
    public required int DownscaleFactor { get; set; } // Downscale by a factor of n
    public required double MotionRatio { get; set; } // n% of the area
}

public enum InferenceMode
{
    MotionBased,
    Continuous
}