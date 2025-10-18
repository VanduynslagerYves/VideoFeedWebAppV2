using Emgu.CV;
using Emgu.CV.BgSegm;

namespace CameraFeed.Processor.Camera.Factories;

public interface IBackgroundSubtractorFactory
{
    IBackgroundSubtractorAdapter Create(BackgroundSubtractorType type);
}

public class BackgroundSubtractorFactory : IBackgroundSubtractorFactory
{
    public IBackgroundSubtractorAdapter Create(BackgroundSubtractorType type)
    {
        return type switch
        {
            /* The history parameter controls how quickly the background model adapts to changes.
             * Larger history (e.g., 1000): The model adapts slowly, so temporary changes (like a person walking by) are less likely to be absorbed into the background.
             * Smaller history (e.g., 50): The model adapts quickly, so new static objects or lighting changes are incorporated into the background faster. */
            BackgroundSubtractorType.MOG2 => new MOG2SubtractorAdapter(new BackgroundSubtractorMOG2(history: 50, shadowDetection: false)),
            BackgroundSubtractorType.CNT => new CNTSubtractorAdapter(new BackgroundSubtractorCNT(minPixelStability: 15, useHistory: true, maxPixelStability: 15*60, isParallel: true)),

            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}

public enum BackgroundSubtractorType
{
    MOG2,
    CNT
}

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
