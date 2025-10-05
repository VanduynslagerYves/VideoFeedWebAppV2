using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using CameraFeed.Processor.Clients.gRPC;
using CameraFeed.Processor.Clients.SignalR;
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
    protected readonly ICameraSignalRclient _signalRclient;
    protected readonly IVideoCaptureFactory _videoCaptureFactory;
    protected readonly IBackgroundSubtractorFactory _backgroundSubtractorFactory;
    protected readonly IObjectDetectionGrpcClient _objectDetectionClient;
    protected readonly WorkerProperties _options;

    protected readonly int _frameSkip = 3;
    protected readonly int _downscaledWidth;
    protected readonly int _downscaledHeight;
    protected readonly int _motionThreshold;

    protected int _frameCounter = 0;
    protected bool _lastMotionResult = false;
    protected Mat _downscaledFrame = new();

    public CameraWorkerBase(WorkerProperties options, ICameraSignalRclient cameraSignalRClient, IVideoCaptureFactory videoCaptureFactory, IBackgroundSubtractorFactory backgroundSubtractorFactory, IObjectDetectionGrpcClient objectDetectionClient)
    {
        _options = options;
        _signalRclient = cameraSignalRClient;
        _videoCaptureFactory = videoCaptureFactory;
        _backgroundSubtractorFactory = backgroundSubtractorFactory;
        _objectDetectionClient = objectDetectionClient;

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

    public int CamId { get; }
    public string CamName { get; }
    public int CamWidth { get; }
    public int CamHeight { get; }

    public abstract Task RunAsync(CancellationToken token);
}

public class CameraWorker(WorkerProperties options, ICameraSignalRclient signalRclient, IVideoCaptureFactory videoCaptureFactory, IBackgroundSubtractorFactory backgroundSubtractorFactory, IObjectDetectionGrpcClient objectDetectionClient,
    ILogger<CameraWorker> logger) : CameraWorkerBase(options, signalRclient, videoCaptureFactory, backgroundSubtractorFactory, objectDetectionClient)
{
    private readonly ILogger<CameraWorker> _logger = logger;

    public override async Task RunAsync(CancellationToken token)
    {
        try
        {
            await _signalRclient.CreateConnectionsAsync(CamName, token);
            
            using var capture = await _videoCaptureFactory.CreateAsync(_options);
            using var subtractor = await _backgroundSubtractorFactory.CreateAsync(type: BackgroundSubtractorType.MOG2);
            using var foregroundMask = new Mat();

            while (!token.IsCancellationRequested)
            {
                using var capturedFrame = capture!.QueryFrame();
                if (capturedFrame == null || capturedFrame.IsEmpty) continue;

                var imageByteArray = ProcessFrame(capturedFrame);
                if (imageByteArray == null) continue; // Drop oversized frames

                if (ShouldRunInference(capturedFrame, foregroundMask, subtractor))
                {
                    imageByteArray = await RunInference(imageByteArray, token);
                }

                await _signalRclient.SendFrame(imageByteArray, CamName, token);
            }

            await _signalRclient.StopAndDisposeConnectionsAsync(CamName, token);
        }
        catch (OperationCanceledException)
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
        switch (_options.Mode)
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

    protected virtual byte[]? ProcessFrame(Mat frame, int quality = 78, int maxSize = 200 * 1024) //200kB max size for image, so we have 100kB overhead in SignalR (max 256kB)
    {
        using var image = frame.ToImage<Bgr, byte>();
        var jpegData = image.ToJpegData(quality);
        if (jpegData == null || jpegData.Length == 0) return null;

        // Drop frame if it exceeds the maximum allowed size (set in SocketServer SignalR options, TODO: get from SocketServer SignalR options)
        if (jpegData.Length >= maxSize)
        {
            _logger.LogWarning("Frame dropped: size {size}kB exceeds max allowed {maxSize}kB.", jpegData.Length / 1024, maxSize / 1024);
            return null;
        }

        return jpegData;
    }
}

public class WorkerProperties
{
    public required InferenceMode Mode { get; set; }
    public required CameraProperties CameraOptions { get; set; }
    public required MotionDetectionProperties MotionDetectionOptions { get; set; }
}

public class CameraProperties
{
    public required int Id { get; set; }
    public required string Name { get; set; }
    public required CameraResolutionProperties Resolution { get; set; }
    public required int Framerate { get; set; }
}

public class CameraResolutionProperties
{
    public required int Width { get; set; }
    public required int Height { get; set; }
}

public class MotionDetectionProperties
{
    public required int DownscaleFactor { get; set; } // Downscale by a factor of n
    public required double MotionRatio { get; set; } // n% of the area
}

public enum InferenceMode
{
    MotionBased,
    Continuous
}