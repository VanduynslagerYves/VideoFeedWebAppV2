using CameraFeed.Processor.Camera.Adapter;
using Emgu.CV;
using Emgu.CV.CvEnum;

namespace CameraFeed.Processor.Camera.Factories;

public interface IVideoCaptureFactory
{
    Task<IVideoCaptureAdapter> CreateAsync(WorkerProperties options);
}

public class VideoCaptureFactory : IVideoCaptureFactory
{
    public Task<IVideoCaptureAdapter> CreateAsync(WorkerProperties options)
    {
        // Initialize VideoCapture with specified camera ID and settings
        // Note: VideoCapture initialization can be blocking, so we offload it to a thread pool thread
        return Task.Run(() =>
        {
            var videoCapture = new VideoCapture(options.CameraOptions.Id);

            if (videoCapture == null || !videoCapture.IsOpened)
                throw new InvalidOperationException($"Camera {options.CameraOptions.Id} could not be initialized.");

            videoCapture.Set(CapProp.FrameWidth, options.CameraOptions.Resolution.Width);
            videoCapture.Set(CapProp.FrameHeight, options.CameraOptions.Resolution.Height);
            videoCapture.Set(CapProp.Fps, options.CameraOptions.Framerate);
            videoCapture.Set(CapProp.FourCC, VideoWriter.Fourcc('M', 'J', 'P', 'G'));

            return new VideoCaptureAdapter(videoCapture) as IVideoCaptureAdapter;
        });
    }
}
