using CameraFeed.Processor.Clients.gRPC;
using CameraFeed.Processor.Extensions;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace CameraFeed.Processor.Camera.Worker;

public interface IFrameProcessor : IDisposable
{
    Task InitializeVideoCaptureAsync();
    Task<byte[]?> QueryAndProcessFrame(int quality = 78, int maxSize = 200 * 1024, CancellationToken cancellationToken = default);
}

public class FrameProcessor : IFrameProcessor
{
    private readonly IVideoCaptureFactory _videoCaptureFactory;
    private readonly IBackgroundSubtractorFactory _backgroundSubtractorFactory;
    private readonly IObjectDetectionGrpcClient _objectDetectionClient;
    private readonly ILogger<FrameProcessor> _logger;
    private readonly WorkerProperties _options;

    private readonly IBackgroundSubtractorAdapter _subtractor;
    private readonly Mat _foregroundMask;
    private readonly Mat _downscaledFrame = new();

    private readonly int _frameSkip = 3;
    private readonly int _downscaledWidth;
    private readonly int _downscaledHeight;
    private readonly int _motionThreshold;

    private int _frameCounter = 0;
    private bool _lastMotionResult = false;

    private VideoCapture? _capture;

    public FrameProcessor(IVideoCaptureFactory videoCaptureFactory, IBackgroundSubtractorFactory backgroundSubtractorFactory, IObjectDetectionGrpcClient objectDetectionClient, WorkerProperties options, ILogger<FrameProcessor> logger)
    {
        _videoCaptureFactory = videoCaptureFactory;
        _backgroundSubtractorFactory = backgroundSubtractorFactory;
        _objectDetectionClient = objectDetectionClient;
        _logger = logger;
        _options = options;

        _subtractor = _backgroundSubtractorFactory.Create(type: BackgroundSubtractorType.MOG2);
        _foregroundMask = new Mat();

        //Setup motion detection parameters
        _downscaledHeight = options.CameraOptions.Resolution.Height / options.MotionDetectionOptions.DownscaleFactor;
        _downscaledWidth = options.CameraOptions.Resolution.Width / options.MotionDetectionOptions.DownscaleFactor;
        int downscaledArea = _downscaledWidth * _downscaledHeight;
        _motionThreshold = (int)(downscaledArea * options.MotionDetectionOptions.MotionRatio);
    }

    public virtual async Task InitializeVideoCaptureAsync()
    {
        _capture = await _videoCaptureFactory.CreateAsync(_options);
    }

    public async Task<byte[]?> QueryAndProcessFrame(int quality = 78, int maxSize = 200 * 1024, CancellationToken cancellationToken = default)
    {
        if (_capture == null || !_capture.IsOpened) return null; //TODO: throw on null

        using var frame = _capture.QueryFrame();
        if(frame == null || frame.IsEmpty) return null;

        using var image = frame.ToImage<Bgr, byte>();
        var jpegData = image.ToJpegData(quality);
        if (jpegData == null || jpegData.Length == 0) return null;

        if(ShouldRunInference(frame))
        {
            // Run inference asynchronously (fire and forget)
            jpegData = await RunInference(jpegData, cancellationToken);
        }

        // Drop frame if it exceeds the maximum allowed size (set in SocketServer SignalR options, TODO: get from SocketServer SignalR options)
        if (jpegData.Length >= maxSize)
        {
            _logger.LogWarning("Frame dropped: size {size}kB exceeds max allowed {maxSize}kB.", jpegData.Length / 1024, maxSize / 1024);
            return null;
        }

        return jpegData;
    }

    private async Task<byte[]> RunInference(byte[] frameData, CancellationToken cancellationToken = default)
    {
        // Call the gRPC object detection
        return await _objectDetectionClient.DetectObjectsAsync(frameData, cancellationToken);
    }

    private bool ShouldRunInference(Mat capturedFrame)
    {
        switch (_options.Mode)
        {
            case InferenceMode.Continuous:
                return true;
            case InferenceMode.MotionBased:
                return MotionDetected(capturedFrame); // Handled separately in the main loop
            default:
                return false;
        }
    }

    private bool MotionDetected(Mat frame)
    {
        if (_frameCounter++ % _frameSkip != 0) return _lastMotionResult; // Only process every n-th frame, by frame skipping, to reduce CPU usage

        // Downscale the frame to reduce the number of pixels to process, improving performance.
        // Uses nearest neighbor interpolation for speed, which is sufficient for motion detection.
        frame.DownscaleTo(
            destination: _downscaledFrame,
            toWidth: _downscaledWidth,
            toHeight: _downscaledHeight,
            interpolationMethod: Inter.Nearest);

        // Apply background subtraction to the downscaled frame.
        // The foregroundMask will contain white pixels where motion is detected.
        _subtractor.Apply(_downscaledFrame, _foregroundMask);

        // Count the number of non-zero (white) pixels in the mask, representing areas of motion.
        int motionPixels = CvInvoke.CountNonZero(_foregroundMask);

        // Determine if the number of motion pixels exceeds the configured threshold.
        bool motionDetected = motionPixels > _motionThreshold;

        // Log only when the motion detection state changes (to reduce log noise).
        if (motionDetected != _lastMotionResult) _logger.LogInformation("Motion {status} with {pixels} pixels at {time}", motionDetected ? "detected" : "stopped", motionPixels, DateTime.Now);

        // Store the result for use in skipped frames and for change detection.
        _lastMotionResult = motionDetected;
        return motionDetected;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _capture?.Dispose();
        _subtractor?.Dispose();
        _foregroundMask?.Dispose();
    }
}
