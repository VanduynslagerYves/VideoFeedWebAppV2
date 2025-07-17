using Emgu.CV;
using Emgu.CV.CvEnum;
using Microsoft.AspNetCore.SignalR;

namespace VideoFeed.Video;

public interface ICameraWorker
{
    Task RunAsync(CancellationToken token);
}

public class CameraWorker(int cameraId, ILogger<CameraWorker> logger, IHubContext<VideoHub> hubContext) : ICameraWorker, IDisposable
{
    private readonly ILogger<CameraWorker> _logger = logger;
    private readonly IHubContext<VideoHub> _hubContext = hubContext; 

    private readonly int _cameraId = cameraId;
    private VideoCapture? _capture;

    private readonly string _imageTempFile = $"temp{cameraId}.jpg";

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _capture?.Dispose();
    }

    public async Task RunAsync(CancellationToken token)
    {
        string workerId = $"Worker {_cameraId}";
        _logger.LogInformation(message: $"{workerId} started.");

        _capture = new VideoCapture(_cameraId);
        _capture.Set(CapProp.FrameWidth, 800);
        _capture.Set(CapProp.FrameHeight, 600);

        while (!token.IsCancellationRequested)
        {
            if (_capture != null && _capture.IsOpened)
            {
                using Mat frame = _capture.QueryFrame(); // Query frame from the capture device
                if (frame != null && !frame.IsEmpty)
                {
                    // Convert the captured frame (Mat) to a JPEG byte array
                    byte[] imageBytes = ConvertFrameToByteArray(frame, _imageTempFile);

                    // Send the image data to SignalR clients
                    await _hubContext.Clients.Group($"camera_{_cameraId}").SendAsync("ReceiveFrame", imageBytes, token);
                }
            }

            await Task.Delay(5, token);
        }

        _logger.LogInformation(message: $"{workerId} stopped.");
    }

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
