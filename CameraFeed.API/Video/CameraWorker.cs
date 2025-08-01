using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Microsoft.AspNetCore.SignalR;
using CameraFeed.API.ApiClients;

namespace CameraFeed.API.Video;

/// <summary>
/// Represents a worker responsible for managing camera operations asynchronously.
/// </summary>
/// <remarks>This interface defines the contract for a camera worker that can be started and monitored for its
/// running state.</remarks>
public interface ICameraWorker
{
    Task RunAsync(CancellationToken token);
    public bool IsRunning { get; }
    public int CameraId { get; }
}

/// <summary>
/// Represents a worker responsible for capturing video frames from a camera and streaming them to SignalR clients.
/// </summary>
/// <remarks>The <see cref="CameraWorker"/> class manages video capture from a specified camera device and streams
/// the captured frames to connected SignalR clients. It supports asynchronous operation and can be controlled using a
/// <see cref="CancellationToken"/> to stop the worker gracefully.</remarks>
/// <param name="cameraId"></param>
/// <param name="logger"></param>
/// <param name="hubContext"></param>
public class CameraWorker(CameraWorkerOptions options, ILogger<CameraWorker> logger, IHumanDetectionApiClient humanDetectionApiClient, IHubContext<CameraHub> hubContext) : ICameraWorker, IDisposable
{
    private readonly IHumanDetectionApiClient _humanDetectionApiClient = humanDetectionApiClient;
    private readonly ILogger<CameraWorker> _logger = logger;
    private readonly IHubContext<CameraHub> _hubContext = hubContext;
    private readonly CameraWorkerOptions _options = options;
    private VideoCapture? _capture;
    private volatile bool _isRunning; //volatile makes this bool threadsafe. if we don't assign this volatile, multiple threads or requests could read/write this value inconsistently.

    public int CameraId { get; } = options.CameraId;
    public bool IsRunning => _isRunning;

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _isRunning = false;
        _capture?.Dispose();
    }

    /// <summary>
    /// Executes the asynchronous worker process for capturing video frames and broadcasting them to SignalR clients.
    /// </summary>
    /// <remarks>This method initializes the video capture device, captures frames in a loop, and sends the
    /// frame data to SignalR clients in the specified group. The method runs until the provided <see
    /// cref="CancellationToken"/> signals cancellation.</remarks>
    /// <param name="token">A <see cref="CancellationToken"/> used to signal cancellation of the operation. The method will stop processing
    /// when cancellation is requested.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task RunAsync(CancellationToken token)
    {
        _isRunning = true;

        string workerId = $"Worker {CameraId}";
        var groupName = $"camera_{CameraId}";

        _logger.LogInformation(message: $"{workerId} started.");

        // Create a background subtractor (MOG2) for motion detection
        var subtractor = new BackgroundSubtractorMOG2();
        var fgMask = new Mat();

        InitCam();

        while (!token.IsCancellationRequested)
        {
            if (!CamReady())
            {
                _logger.LogWarning(message: $"{workerId} could not open camera with ID {CameraId}. Retrying...");
                _capture?.Dispose();
                InitCam();
                if (!CamReady())
                {
                    await Task.Delay(1000, token); // Wait a bit before retrying
                    continue;
                }
            }

            using var capturedFrame = _capture!.QueryFrame(); // Capture a frame
            if ((capturedFrame == null || capturedFrame.IsEmpty)) continue;

            // Convert the captured frame (Mat) to a byte array
            var imageByteArray = ConvertFrameToByteArray(capturedFrame);

            if (_options.UseMotionDetection && MotionDetected(capturedFrame, fgMask, subtractor))
            {
                imageByteArray = await _humanDetectionApiClient.DetectHumansAsync(imageByteArray);
            }

            // Send the image byte data to SignalR clients
            await _hubContext.Clients.Group(groupName).SendAsync(method: "ReceiveImgBytes", imageByteArray, token);

            await Task.Delay(5, token);
        }

        _isRunning = false;

        _logger.LogInformation(message: $"{workerId} stopped.");
    }

    /// <summary>
    /// Detects motion in the given video frame using a background subtraction algorithm.
    /// </summary>
    /// <remarks>This method applies the specified background subtractor to the provided video frame to
    /// generate a foreground mask. It then counts the non-zero pixels in the mask to determine if motion is present. If
    /// the number of motion pixels exceeds a predefined threshold, a message indicating motion detection is logged to
    /// the console.</remarks>
    /// <param name="frame">The current video frame to analyze for motion.</param>
    /// <param name="fgMask">The foreground mask where detected motion will be highlighted.</param>
    /// <param name="subtractor">The background subtractor used to differentiate moving objects from the background.</param>
    private static bool MotionDetected(Mat frame, Mat fgMask, BackgroundSubtractorMOG2 subtractor)
    {
        // Apply the background subtractor to the current frame
        subtractor.Apply(frame, fgMask);

        // Count non-zero pixels in the foreground mask to detect motion
        int motionPixels = CvInvoke.CountNonZero(fgMask);
        if (motionPixels > 3000)
        {
            Console.WriteLine($"Motion Detected at {DateTime.Now}");
            return true;
        }

        return false;
    }

    /// <summary>
    /// Initializes the camera with specified settings for frame width, height, frames per second, and codec.
    /// </summary>
    /// <remarks>This method configures the camera to capture video at a resolution of 640x480 pixels, with a
    /// frame rate of 30 frames per second, using the MJPEG codec if supported.</remarks>
    private void InitCam()
    {
        _capture = new VideoCapture(CameraId);
        _capture.Set(CapProp.FrameWidth, 640);
        _capture.Set(CapProp.FrameHeight, 480);
        _capture.Set(CapProp.Fps, 15);
        _capture.Set(CapProp.FourCC, VideoWriter.Fourcc('M', 'J', 'P', 'G')); // MJPEG if supported
    }

    /// <summary>
    /// Converts a video frame represented as a <see cref="Mat"/> object into a JPEG-encoded byte array.
    /// </summary>
    /// <remarks>This method uses the <see cref="Mat.ToImage{TColor, TDepth}"/> method to convert the frame to
    /// an image format and then encodes it as a JPEG. The resulting byte array can be used for storage, transmission,
    /// or further processing.</remarks>
    /// <param name="frame">The video frame to convert. Must be a valid <see cref="Mat"/> object.</param>
    /// <param name="quality">The quality level of the JPEG encoding, ranging from 0 to 100. Higher values produce better image quality but
    /// result in larger file sizes. The default value is 70.</param>
    /// <returns>A byte array containing the JPEG-encoded representation of the input frame.</returns>
    private static byte[] ConvertFrameToByteArray(Mat frame, int quality = 70)
    {
        // Encode the Mat to JPEG directly into a byte array
        var imageBytes = frame.ToImage<Bgr, byte>().ToJpegData(quality);
        return imageBytes;
    }

    private bool CamReady()
    {
        return _capture != null && _capture.IsOpened;
    }

    [Obsolete("Use other more efficient method instead")]
    private static byte[] ConvertFrameToByteArray(Mat frame, string imageTempFile)
    {
        // Create a memory stream to hold the image data
        using var ms = new MemoryStream();
        // Save the frame as a temporary image (in JPEG format)
        CvInvoke.Imwrite(imageTempFile, frame);

        // Now load it back into the memory stream
        byte[] imageBytes = File.ReadAllBytes(imageTempFile);
        ms.Write(imageBytes, 0, imageBytes.Length);

        // Return the byte array of the image
        return ms.ToArray();
    }
}

public class CameraWorkerOptions
{
    public required int CameraId { get; set; }
    public required bool UseMotionDetection { get; set; } = false;
}