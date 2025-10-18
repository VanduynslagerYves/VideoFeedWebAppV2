using CameraFeed.Processor.Camera.Adapter;
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