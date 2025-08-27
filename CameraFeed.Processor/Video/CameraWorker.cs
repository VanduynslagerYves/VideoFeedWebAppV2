using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Microsoft.AspNetCore.SignalR;
using CameraFeed.Processor.Services.gRPC;

namespace CameraFeed.Processor.Video;

public interface ICameraWorker
{
    Task RunAsync(CancellationToken token);
    //public bool IsRunning { get; }
    int CameraId { get; }
    void ReleaseCapture();
}

public abstract class CameraWorkerBase(StartWorkerOptions options,
    IVideoCaptureFactory videoCaptureFactory, IBackgroundSubtractorFactory backgroundSubtractorFactory, IObjectDetectionGrpcClient objectDetectioniClient,
    IHubContext<CameraHub> hubContext) : ICameraWorker
{
    protected readonly IObjectDetectionGrpcClient _objectDetectionClient = objectDetectioniClient;
    protected readonly IHubContext<CameraHub> _hubContext = hubContext;
    protected readonly IVideoCaptureFactory _videoCaptureFactory = videoCaptureFactory;
    protected readonly IBackgroundSubtractorFactory _backgroundSubtractorFactory = backgroundSubtractorFactory;

    protected VideoCapture? _capture;
    protected readonly StartWorkerOptions _options = options;

    //protected volatile bool _isRunning; //volatile makes this bool threadsafe. if we don't assign this volatile, multiple threads or requests could read/write this value inconsistently.

    //public bool IsRunning => _isRunning;

    public int CameraId { get; } = options.CameraId;

    protected string NotifyImageGroup => $"camera_{CameraId}";

    public abstract Task RunAsync(CancellationToken token);

    public abstract void ReleaseCapture();
}

public class CameraWorker(StartWorkerOptions options,
    IVideoCaptureFactory videoCaptureFactory, IBackgroundSubtractorFactory backgroundSubtractorFactory, IObjectDetectionGrpcClient objectDetectionClient,
    ILogger<CameraWorker> logger, IHubContext<CameraHub> hubContext) : CameraWorkerBase(options, videoCaptureFactory, backgroundSubtractorFactory, objectDetectionClient, hubContext)
{
    private readonly ILogger<CameraWorker> _logger = logger;

    public override async Task RunAsync(CancellationToken token)
    {
        //_isRunning = true;

        try
        {
            InitCamera();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to initialize camera with ID {CameraId}.", _options.CameraId);
            //_isRunning = false;
            _capture?.Dispose();
            return;
        }

        var subtractor = _backgroundSubtractorFactory.Create();
        var fgMask = new Mat();

        while (!token.IsCancellationRequested)
        {
            using var capturedFrame = _capture!.QueryFrame();
            if (capturedFrame == null || capturedFrame.IsEmpty) continue;

            var imageByteArray = ConvertFrameToByteArray(capturedFrame);

            if (_options.UseContinuousInference || (_options.UseMotionDetection && MotionDetected(capturedFrame, fgMask, subtractor)))
            {
                imageByteArray = await RunInference(imageByteArray, token);
            }

            await SendFrameToClientsAsync(imageByteArray, token);
        }
    }

    private void InitCamera()
    {
        _capture = _videoCaptureFactory.Create(_options.CameraId);
        if (_capture == null || !_capture.IsOpened)
        {
            throw new InvalidOperationException($"Camera with ID {_options.CameraId} could not be initialized. Ensure the camera is connected and accessible.");
        }

        _capture.Set(CapProp.FrameWidth, _options.CameraOptions.Resolution.Width);
        _capture.Set(CapProp.FrameHeight, _options.CameraOptions.Resolution.Height);
        _capture.Set(CapProp.Fps, _options.CameraOptions.Framerate);
        _capture.Set(CapProp.FourCC, VideoWriter.Fourcc('M', 'J', 'P', 'G')); // MJPEG if supported
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

    public override void ReleaseCapture()
    {
        _capture?.Dispose();
    }
}

public class CameraOptions
{
    public required CameraResolution Resolution { get; set; }
    public required int Framerate { get; set; }
}