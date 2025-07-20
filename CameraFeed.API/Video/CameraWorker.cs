using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Microsoft.AspNetCore.SignalR;

namespace CameraFeed.API.Video;

public interface ICameraWorker
{
    Task RunAsync(CancellationToken token);
    public bool IsRunning { get; }
    public int CameraId { get; }
}

public class CameraWorker(int cameraId, ILogger<CameraWorker> logger, IHubContext<CameraHub> hubContext) : ICameraWorker, IDisposable
{
    private readonly ILogger<CameraWorker> _logger = logger;
    private readonly IHubContext<CameraHub> _hubContext = hubContext;

    public int CameraId { get; } = cameraId;

    private VideoCapture? _capture;

    private volatile bool _isRunning; //volatile makes this bool threadsafe. if we don't assign this volatile, multiple threads or requests could read/write this value inconsistently.
    public bool IsRunning => _isRunning;

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _isRunning = false;
        _capture?.Dispose();
    }

    public async Task RunAsync(CancellationToken token)
    {
        _isRunning = true;

        string workerId = $"Worker {CameraId}";
        var groupName = $"camera_{CameraId}";

        _logger.LogInformation(message: $"{workerId} started.");

        //Define capture device
        _capture = new VideoCapture(CameraId);
        _capture.Set(CapProp.FrameWidth, 800);
        _capture.Set(CapProp.FrameHeight, 600);
        _capture.Set(CapProp.Fps, 30);
        _capture.Set(CapProp.FourCC, VideoWriter.Fourcc('M', 'J', 'P', 'G')); // MJPEG if supported

        while (!token.IsCancellationRequested)
        {
            if (_capture != null && _capture.IsOpened)
            {
                using var capturedFrame = _capture.QueryFrame(); // Capture a frame
                if (capturedFrame != null && !capturedFrame.IsEmpty)
                {
                    // Convert the captured frame (Mat) to a byte array
                    byte[] imageBytes = ConvertFrameToByteArray(capturedFrame);

                    // Send the image byte data to SignalR clients
                    await _hubContext.Clients.Group(groupName).SendAsync(method: "ReceiveImgBytes", imageBytes, token);
                }
            }

            await Task.Delay(5, token);
        }

        _isRunning = false;

        _logger.LogInformation(message: $"{workerId} stopped.");
    }

    private static byte[] ConvertFrameToByteArray(Mat frame, int quality = 70)
    {
        // Encode the Mat to JPEG directly into a byte array
        var imageBytes = frame.ToImage<Bgr, byte>().ToJpegData(quality);
        return imageBytes;
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