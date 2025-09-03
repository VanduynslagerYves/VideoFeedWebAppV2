using Emgu.CV;
using Emgu.CV.CvEnum;

namespace CameraFeed.Processor.Extensions;

public static class MatExtensions
{
    /// <summary>
    /// Downscales a Mat to specified dimensions
    /// </summary>
    public static Mat Downscale(this Mat mat, int toWidth, int toHeight, Inter interpolationMethod = Inter.Nearest)
    {
        var downscaled = new Mat();
        CvInvoke.Resize(src: mat, dst: downscaled, dsize: new System.Drawing.Size(toWidth, toHeight), interpolation: interpolationMethod);
        return downscaled;
    }

    /// <summary>
    /// Memory-efficient version that reuses the destination Mat
    /// </summary>
    public static void DownscaleTo(this Mat mat, Mat destination, int toWidth, int toHeight, Inter interpolationMethod = Inter.Nearest)
    {
        CvInvoke.Resize(src: mat, dst: destination, dsize: new System.Drawing.Size(toWidth, toHeight),
            interpolation: interpolationMethod);
    }

    /// <summary>
    /// Downscales a Mat by a specified factor (e.g. 0.5 for half size)
    /// </summary>
    public static Mat DownscaleByFactor(this Mat mat, double factor, Inter interpolationMethod = Inter.Nearest)
    {
        var downscaled = new Mat();
        CvInvoke.Resize(src: mat, dst: downscaled, dsize: new System.Drawing.Size(0, 0),
            fx: factor, fy: factor, interpolation: interpolationMethod);
        return downscaled;
    }

    /// <summary>
    /// Downscales a Mat using a pyramid approach for better quality/performance on large scale reductions
    /// </summary>
    public static Mat PyramidDownscale(this Mat mat, int toWidth, int toHeight, Inter interpolationmethod = Inter.Nearest)
    {
        // For substantial downscaling (more than 2x reduction), pyramid approach works better
        Mat current = mat.Clone();

        // While current dimensions are more than 2x the target, use pyrDown (which does 2x reduction with Gaussian filtering)
        while (current.Width > toWidth * 2 && current.Height > toHeight * 2)
        {
            var smaller = new Mat();
            CvInvoke.PyrDown(current, smaller);
            current.Dispose();
            current = smaller;
        }

        // Final resize to exact dimensions if needed
        if (current.Width != toWidth || current.Height != toHeight)
        {
            var final = new Mat();
            CvInvoke.Resize(current, final, new System.Drawing.Size(toWidth, toHeight), 0, 0, interpolationmethod);
            current.Dispose();
            current = final;
        }

        return current;
    }
}
