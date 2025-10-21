using Emgu.CV;

namespace CameraFeed.Processor.Camera.Adapter;

/// <summary>
/// Represents an adapter for video capture devices, providing functionality to query video frames and manage the
/// device's state.
/// </summary>
/// <remarks>This interface is designed to abstract video capture operations, such as checking if the device is
/// open and retrieving video frames. Implementations of this interface should ensure proper resource management,
/// particularly by calling <see cref="Dispose"/> when the adapter is no longer needed.
/// At the time of writing, this adapter is used for mocking the VideoCapture object for unit tests.</remarks>
public interface IVideoCaptureAdapter
{
    bool IsOpened { get; }
    Mat? QueryFrame();
    void Dispose();
}

public class VideoCaptureAdapter(VideoCapture capture) : IVideoCaptureAdapter
{
    private readonly VideoCapture _capture = capture;

    public bool IsOpened => _capture.IsOpened;
    public Mat? QueryFrame() => _capture.QueryFrame();
    public void Dispose() => _capture.Dispose();
}
