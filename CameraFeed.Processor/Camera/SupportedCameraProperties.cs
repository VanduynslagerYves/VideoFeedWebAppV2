using CameraFeed.Processor.Camera.Worker;

namespace CameraFeed.Processor.Camera;

public static class SupportedCameraProperties
{
    public static readonly Dictionary<string, CameraResolution> Resolutions = new()
    {
        { "1080p", new CameraResolution{Width=1920, Height=1080} },
        { "900p", new CameraResolution{Width=1600, Height=900} },
        { "720p", new CameraResolution{Width=1280, Height=720} },
        { "576p", new CameraResolution{Width=1024, Height=576} },
        { "480p", new CameraResolution{Width=854, Height=480} },
        { "360p", new CameraResolution{Width=640, Height=360} },
        { "240p", new CameraResolution{Width=426, Height=240} }
    };

    public static CameraResolution GetResolutionById(string resolutionKey)
    {
        if (Resolutions.TryGetValue(resolutionKey, out var resolution))
        {
            return resolution;
        }
        return Resolutions["1080p"];
    }

    public static readonly List<int> FrameRates = [15, 20, 25, 30];

    public static readonly List<string> ResolutionKeys = [.. Resolutions.Keys];
}