namespace VideoFeed.Video;

using Emgu.CV;
using Emgu.CV.CvEnum;
using Microsoft.AspNetCore.SignalR;
using System.IO;
using System.Threading;

public class VideoCaptureService(IHubContext<VideoStreamHub> hubContext)
{
    private readonly IHubContext<VideoStreamHub> _hubContext = hubContext;
    private VideoCapture? _capture;
    private CancellationTokenSource? _cancellationTokenSource;

    public void StartCapture()
    {
        // Initialize the video capture (use device 0 by default)
        _capture = new VideoCapture(0); // 0 is usually the default camera

        // Set video capture properties (optional)
        _capture.Set(CapProp.FrameWidth, 640);  // Set frame width (optional)
        _capture.Set(CapProp.FrameHeight, 480); // Set frame height (optional)

        // Start the capture process in a separate thread
        _cancellationTokenSource = new CancellationTokenSource();
        var captureThread = new Thread(CaptureFrames);
        captureThread.Start(_cancellationTokenSource.Token);
    }

    private void CaptureFrames(object? obj)
    {
        if (obj == null) return;
        var cancellationToken = (CancellationToken)obj;

        while (!cancellationToken.IsCancellationRequested)
        {
            if (_capture != null && _capture.IsOpened)
            {
                using Mat frame = _capture.QueryFrame();  // Query frame from the capture device
                if (frame != null && !frame.IsEmpty)
                {
                    // Convert the captured frame (Mat) to a JPEG byte array
                    byte[] imageBytes = ConvertFrameToByteArray(frame);

                    // Send the image data to SignalR clients
                    _hubContext.Clients.All.SendAsync("ReceiveVideo", imageBytes);
                }
            }

            // Sleep for a short time to control frame capture rate
            Thread.Sleep(10);  // Adjust the delay as necessary for frame rate
        }
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

    public void StopCapture()
    {
        _cancellationTokenSource?.Cancel();
        _capture?.Release();  // Release resources properly
    }
}

