using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Microsoft.AspNetCore.SignalR;
using CameraFeed.API.Services;

namespace CameraFeed.API.Video;

public interface ICameraWorker
{
    Task RunAsync(CancellationToken token);
    public bool IsRunning { get; }
    public int CameraId { get; }
}

public abstract class CameraWorkerBase(CameraWorkerOptions options,
    IVideoCaptureFactory videoCaptureFactory, IBackgroundSubtractorFactory backgroundSubtractorFactory, IHumanDetectionApiClient humanDetectionApiClient,
    IHubContext<CameraHub> hubContext) : ICameraWorker
{
    protected readonly IHumanDetectionApiClient _humanDetectionApiClient = humanDetectionApiClient;
    protected readonly IHubContext<CameraHub> _hubContext = hubContext;
    protected readonly IVideoCaptureFactory _videoCaptureFactory = videoCaptureFactory;
    protected readonly IBackgroundSubtractorFactory _backgroundSubtractorFactory = backgroundSubtractorFactory;

    protected readonly CameraWorkerOptions _options = options;

    protected volatile bool _isRunning; //volatile makes this bool threadsafe. if we don't assign this volatile, multiple threads or requests could read/write this value inconsistently.

    public bool IsRunning => _isRunning;

    public int CameraId { get; } = options.CameraId;

    protected string WorkerId => $"Worker {CameraId}";
    protected string GroupName => $"camera_{CameraId}";

    protected VideoCapture? Capture;
    protected BackgroundSubtractorMOG2 Subtractor => _backgroundSubtractorFactory.Create();
    protected Mat FgMask { get; } = new Mat();

    public abstract Task RunAsync(CancellationToken token);
}

public class CameraWorker(CameraWorkerOptions options,
    IVideoCaptureFactory videoCaptureFactory, IBackgroundSubtractorFactory backgroundSubtractorFactory, IHumanDetectionApiClient humanDetectionApiClient, 
    ILogger<CameraWorker> logger, IHubContext<CameraHub> hubContext) : CameraWorkerBase(options, videoCaptureFactory, backgroundSubtractorFactory, humanDetectionApiClient, hubContext), IDisposable
{
    private readonly ILogger<CameraWorker> _logger = logger;

    public override async Task RunAsync(CancellationToken token)
    {
        _isRunning = true;
        InitCam();

        while (!token.IsCancellationRequested)
        {
            //if (!CamReady())
            //{
            //    await HandleCameraNotReadyAsync(WorkerId, token);
            //    continue;
            //}

            using var capturedFrame = CaptureFrame();
            if (!IsFrameValid(capturedFrame)) continue;

            var imageByteArray = ConvertFrameToByteArray(capturedFrame!);

            if (ShouldRunInference(capturedFrame!))//, fgMask))//, Subtractor))
            {
                imageByteArray = await RunInference(imageByteArray);
            }

            await SendFrameToClientsAsync(imageByteArray, token);
        }
    }

    protected virtual bool MotionDetected(Mat frame)//, Mat fgMask)//, BackgroundSubtractorMOG2 subtractor)
    {
        // Apply the background subtractor to the current frame
        Subtractor.Apply(frame, FgMask);

        // Count non-zero pixels in the foreground mask to detect motion
        int motionPixels = CvInvoke.CountNonZero(FgMask);
        if (motionPixels > 2000) //3000
        {
            var timeStamp = DateTime.Now;
            _logger.LogInformation(message: "Motion detected in frame with {motionPixels} pixels at {timeStamp}", motionPixels, timeStamp);

            return true;
        }

        return false;
    }

    private void InitCam()
    {
        Capture = _videoCaptureFactory.Create(_options.CameraId);

        Capture.Set(CapProp.FrameWidth, 1920);
        Capture.Set(CapProp.FrameHeight, 1080);
        //_capture.Set(CapProp.Fps, 30);
        Capture.Set(CapProp.FourCC, VideoWriter.Fourcc('M', 'J', 'P', 'G')); // MJPEG if supported
    }

    protected virtual byte[] ConvertFrameToByteArray(Mat frame, int quality = 70)
    {
        // Encode the Mat to JPEG directly into a byte array
        var imageBytes = frame.ToImage<Bgr, byte>().ToJpegData(quality);
        return imageBytes;
    }

    protected virtual bool CamReady()
    {
        return Capture != null && Capture.IsOpened;
    }

    //protected virtual async Task HandleCameraNotReadyAsync(string workerId, CancellationToken token)
    //{
    //    _logger.LogWarning("{workerId} could not open camera with ID {CameraId}. Retrying...", workerId, CameraId);
    //    _capture?.Dispose();

    //    InitCam();

    //    if (!CamReady())
    //    {
    //        await Task.Delay(1000, token);
    //    }
    //}

    protected virtual Mat? CaptureFrame()
    {
        return Capture?.QueryFrame();
    }

    protected virtual bool IsFrameValid(Mat? frame)
    {
        return frame != null && !frame.IsEmpty;
    }

    protected virtual bool ShouldRunInference(Mat frame)//, Mat fgMask)//, BackgroundSubtractorMOG2 subtractor)
    {
        return _options.UseContinuousInference ||
               (_options.UseMotionDetection && MotionDetected(frame));//, fgMask));//, subtractor));
    }

    protected virtual async Task<byte[]> RunInference(byte[] imageByteArray)
    {
        // TODO: circuitbreaker pattern when api is not available
        return await _humanDetectionApiClient.DetectHumansAsync(imageByteArray);
    }

    protected virtual async Task SendFrameToClientsAsync(byte[] imageByteArray, CancellationToken token)
    {
        await _hubContext.Clients.Group(GroupName).SendAsync("ReceiveImgBytes", imageByteArray, token);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _isRunning = false;
        Capture?.Dispose();
    }
}

public class CameraWorkerOptions
{
    public required int CameraId { get; set; }
    public required bool UseContinuousInference { get; set; } = false;
    public required bool UseMotionDetection { get; set; } = false;
}