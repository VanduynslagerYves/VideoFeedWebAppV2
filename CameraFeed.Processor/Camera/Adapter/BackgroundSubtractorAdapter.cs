using Emgu.CV;
using Emgu.CV.BgSegm;

namespace CameraFeed.Processor.Camera.Adapter;

/// <summary>
/// Defines an adapter for applying background subtraction to video frames.
/// </summary>
public interface IBackgroundSubtractorAdapter : IDisposable
{
    void Apply(Mat frame, Mat mask);
}

/// <summary>
/// Adapts the <see cref="BackgroundSubtractorMOG2"/> class to the <see cref="IBackgroundSubtractorAdapter"/> interface.
/// </summary>
/// <remarks>This adapter provides a way to use the <see cref="BackgroundSubtractorMOG2"/> background subtraction
/// algorithm through the <see cref="IBackgroundSubtractorAdapter"/> interface. The algorithm is known for its balance
/// of speed and accuracy and includes support for shadow detection. However, it may be less effective in highly dynamic
/// background scenarios.</remarks>
/// <param name="mog2"></param>
public class MOG2SubtractorAdapter(BackgroundSubtractorMOG2 mog2) : IBackgroundSubtractorAdapter
{
    public void Apply(Mat frame, Mat mask)
    {
        mog2.Apply(frame, mask);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        mog2.Dispose();
    }
}

/// <summary>
/// Adapts the <see cref="BackgroundSubtractorCNT"/> class to the <see cref="IBackgroundSubtractorAdapter"/> interface.
/// </summary>
/// <remarks>This adapter provides a bridge for using the <see cref="BackgroundSubtractorCNT"/> implementation 
/// with the <see cref="IBackgroundSubtractorAdapter"/> interface. The <see cref="BackgroundSubtractorCNT"/> is known
/// for its high performance and low memory usage,  but it may be less accurate and less robust to noise or lighting
/// changes compared to other background subtraction methods.</remarks>
/// <param name="cnt"></param>
public class CNTSubtractorAdapter(BackgroundSubtractorCNT cnt) : IBackgroundSubtractorAdapter
{
    public void Apply(Mat frame, Mat mask)
    {
        cnt.Apply(frame, mask);
    }
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        cnt.Dispose();
    }
}

