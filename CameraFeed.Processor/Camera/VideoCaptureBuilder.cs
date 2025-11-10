using CameraFeed.Processor.Camera.Adapter;
using Emgu.CV;
using Emgu.CV.CvEnum;

namespace CameraFeed.Processor.Camera;

public interface IVideoCaptureBuilder
{
    VideoCaptureBuilder WithResolution(int width, int height);
    VideoCaptureBuilder WithFramerate(int framerate);
    VideoCaptureBuilder WithApi(VideoCapture.API api);
    VideoCaptureBuilder WithFourCC(string fourcc);
    VideoCaptureBuilder WithBrightness(double value);
    VideoCaptureBuilder WithContrast(double value);
    VideoCaptureBuilder WithSaturation(double value);
    VideoCaptureBuilder WithHue(double value);
    VideoCaptureBuilder WithGain(double value);
    VideoCaptureBuilder WithExposure(double value);
    VideoCaptureBuilder WithAutoExposure(double value);
    VideoCaptureBuilder WithWhiteBalanceBlueU(double value);
    VideoCaptureBuilder WithWhiteBalanceRedV(double value);
    VideoCaptureBuilder WithBufferSize(int value);
    VideoCaptureBuilder WithConvertRgb(bool value);
    VideoCaptureBuilder WithZoom(double value);
    VideoCaptureBuilder WithFocus(double value);
    VideoCaptureBuilder WithBacklight(double value);

    Task<IVideoCaptureAdapter> BuildAsync();
}

public class VideoCaptureBuilder(int cameraId) : IVideoCaptureBuilder
{
    private readonly int _cameraId = cameraId;

    private int? _width;
    private int? _height;
    private int? _framerate;
    private int? _fourcc;
    private VideoCapture.API _api = VideoCapture.API.Any;

    private double? _brightness;
    private double? _contrast;
    private double? _saturation;
    private double? _hue;
    private double? _gain;
    private double? _exposure;
    private double? _autoExposure;
    private double? _whiteBalanceBlueU;
    private double? _whiteBalanceRedV;
    private int? _bufferSize;
    private bool? _convertRgb;
    private double? _zoom;
    private double? _focus;
    private double? _backlight;

    public VideoCaptureBuilder WithResolution(int width, int height)
    {
        _width = width;
        _height = height;
        return this;
    }

    public VideoCaptureBuilder WithFramerate(int framerate)
    {
        _framerate = framerate;
        return this;
    }

    public VideoCaptureBuilder WithApi(VideoCapture.API api)
    {
        _api = api;
        return this;
    }

    public VideoCaptureBuilder WithFourCC(string fourcc)
    {
        _fourcc = VideoWriter.Fourcc(
            fourcc[0], fourcc[1], fourcc[2], fourcc[3]);
        return this;
    }

    public VideoCaptureBuilder WithBrightness(double value)
    {
        _brightness = value;
        return this;
    }

    public VideoCaptureBuilder WithContrast(double value)
    {
        _contrast = value;
        return this;
    }

    public VideoCaptureBuilder WithSaturation(double value)
    {
        _saturation = value;
        return this;
    }

    public VideoCaptureBuilder WithHue(double value)
    {
        _hue = value;
        return this;
    }

    public VideoCaptureBuilder WithGain(double value)
    {
        _gain = value;
        return this;
    }

    public VideoCaptureBuilder WithExposure(double value)
    {
        _exposure = value;
        return this;
    }

    public VideoCaptureBuilder WithAutoExposure(double value)
    {
        _autoExposure = value;
        return this;
    }

    public VideoCaptureBuilder WithWhiteBalanceBlueU(double value)
    {
        _whiteBalanceBlueU = value;
        return this;
    }

    public VideoCaptureBuilder WithWhiteBalanceRedV(double value)
    {
        _whiteBalanceRedV = value;
        return this;
    }

    public VideoCaptureBuilder WithBufferSize(int value)
    {
        _bufferSize = value;
        return this;
    }

    public VideoCaptureBuilder WithConvertRgb(bool value)
    {
        _convertRgb = value;
        return this;
    }

    public VideoCaptureBuilder WithZoom(double value)
    {
        _zoom = value;
        return this;
    }

    public VideoCaptureBuilder WithFocus(double value)
    {
        _focus = value;
        return this;
    }

    public VideoCaptureBuilder WithBacklight(double value)
    {
        _backlight = value;
        return this;
    }

    public Task<IVideoCaptureAdapter> BuildAsync()
    {
        // Note: VideoCapture initialization can be blocking, so we offload it to a thread
        return Task.Run(() =>
        {
            var capture = new VideoCapture(_cameraId, _api);

            if (!capture.IsOpened)
                throw new InvalidOperationException($"Camera {_cameraId} could not be initialized.");

            //TODO: use CQS pattern to set properties ?
            if (_width.HasValue)
                capture.Set(CapProp.FrameWidth, _width.Value);
            if (_height.HasValue)
                capture.Set(CapProp.FrameHeight, _height.Value);
            if (_framerate.HasValue)
                capture.Set(CapProp.Fps, _framerate.Value);
            if (_fourcc.HasValue)
                capture.Set(CapProp.FourCC, _fourcc.Value);
            if (_brightness.HasValue)
                capture.Set(CapProp.Brightness, _brightness.Value);
            if (_contrast.HasValue)
                capture.Set(CapProp.Contrast, _contrast.Value);
            if (_saturation.HasValue)
                capture.Set(CapProp.Saturation, _saturation.Value);
            if (_hue.HasValue)
                capture.Set(CapProp.Hue, _hue.Value);
            if (_gain.HasValue)
                capture.Set(CapProp.Gain, _gain.Value);
            if (_exposure.HasValue)
                capture.Set(CapProp.Exposure, _exposure.Value);
            if (_autoExposure.HasValue)
                capture.Set(CapProp.AutoExposure, _autoExposure.Value);
            if (_whiteBalanceBlueU.HasValue)
                capture.Set(CapProp.WhiteBalanceBlueU, _whiteBalanceBlueU.Value);
            if (_whiteBalanceRedV.HasValue)
                capture.Set(CapProp.WhiteBalanceRedV, _whiteBalanceRedV.Value);
            if (_bufferSize.HasValue)
                capture.Set(CapProp.Buffersize, _bufferSize.Value);
            if (_convertRgb.HasValue)
                capture.Set(CapProp.ConvertRgb, _convertRgb.Value ? 1 : 0);
            if (_zoom.HasValue)
                capture.Set(CapProp.Zoom, _zoom.Value);
            if (_focus.HasValue)
                capture.Set(CapProp.Focus, _focus.Value);
            if (_backlight.HasValue)
                capture.Set(CapProp.Backlight, _backlight.Value);

            return new VideoCaptureAdapter(capture) as IVideoCaptureAdapter;
        });
    }
}