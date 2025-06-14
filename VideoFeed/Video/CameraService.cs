using Emgu.CV;
using Emgu.CV.CvEnum;
using Microsoft.AspNetCore.SignalR;

namespace VideoFeed.Video;

public class CameraService(IHubContext<VideoHub> hub) : BackgroundService
{
    private readonly IHubContext<VideoHub> _hub = hub;
    private VideoCapture? _capture;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _capture = new VideoCapture(0);
        _capture.Set(CapProp.FrameWidth, 640);
        _capture.Set(CapProp.FrameHeight, 480);

        while (!stoppingToken.IsCancellationRequested)
        {
            if (_capture != null && _capture.IsOpened)
            {
                using Mat frame = _capture.QueryFrame();  // Query frame from the capture device
                if (frame != null && !frame.IsEmpty)
                {
                    // Convert the captured frame (Mat) to a JPEG byte array
                    byte[] imageBytes = ConvertFrameToByteArray(frame);

                    // Send the image data to SignalR clients
                    await _hub.Clients.All.SendAsync("ReceiveFrame", imageBytes, stoppingToken);
                }
            }

            await Task.Delay(5, stoppingToken);
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        GC.SuppressFinalize(this);
        _capture?.Dispose();
    }

    private static byte[] ConvertFrameToByteArray(Mat frame)
    {
        // Create a memory stream to hold the image data
        using var ms = new MemoryStream();
        // Save the frame as a temporary image (in JPEG format)
        CvInvoke.Imwrite("temp.jpg", frame);  // Save to temporary file (JPEG)

        // Now load it back into the memory stream
        byte[] imageBytes = File.ReadAllBytes("temp.jpg");
        ms.Write(imageBytes, 0, imageBytes.Length);

        // Return the byte array of the image
        return ms.ToArray();
    }
}
