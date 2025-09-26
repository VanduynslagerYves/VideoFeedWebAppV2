using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using CameraFeed.Processor.Clients.gRPC;
using CameraFeed.Processor.Extensions;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.SignalR;

namespace CameraFeed.Processor.Camera.Worker;

public interface ICameraWorker
{
    Task RunAsync(CancellationToken token);
    int CamId { get; }
    string CamName { get; }
    int CamWidth { get; }
    int CamHeight { get; }
}

public abstract class CameraWorkerBase : ICameraWorker
{
    protected readonly IHubConnectionFactory _hubConnectionFactory;
    protected readonly IVideoCaptureFactory _videoCaptureFactory;
    protected readonly IBackgroundSubtractorFactory _backgroundSubtractorFactory;
    protected readonly IObjectDetectionGrpcClient _objectDetectionClient;
    protected readonly WorkerProperties _options;

    protected readonly int _frameSkip = 3;
    protected readonly int _downscaledWidth;
    protected readonly int _downscaledHeight;
    protected readonly int _motionThreshold;

    protected int _frameCounter = 0;
    protected bool _lastMotionResult = false;
    protected Mat _downscaledFrame = new();

    public CameraWorkerBase(WorkerProperties options, IHubConnectionFactory hubConnectionFactory, IVideoCaptureFactory videoCaptureFactory, IBackgroundSubtractorFactory backgroundSubtractorFactory, IObjectDetectionGrpcClient objectDetectionClient)
    {
        _options = options;
        _hubConnectionFactory = hubConnectionFactory;
        _videoCaptureFactory = videoCaptureFactory;
        _backgroundSubtractorFactory = backgroundSubtractorFactory;
        _objectDetectionClient = objectDetectionClient;

        CamId = options.CameraOptions.Id;
        CamName = options.CameraOptions.Name;
        CamWidth = options.CameraOptions.Resolution.Width;
        CamHeight = options.CameraOptions.Resolution.Height;

        //Setup motion detection parameters
        _downscaledHeight = options.CameraOptions.Resolution.Height / options.MotionDetectionOptions.DownscaleFactor;
        _downscaledWidth = options.CameraOptions.Resolution.Width / options.MotionDetectionOptions.DownscaleFactor;
        int downscaledArea = _downscaledWidth * _downscaledHeight;
        _motionThreshold = (int)(downscaledArea * options.MotionDetectionOptions.MotionRatio);
    }

    public int CamId { get; }
    public string CamName { get; }
    public int CamWidth { get; }
    public int CamHeight { get; }

    public abstract Task RunAsync(CancellationToken token);
}

public class CameraWorker(WorkerProperties options, IHubConnectionFactory hubConnectionFactory, IVideoCaptureFactory videoCaptureFactory, IBackgroundSubtractorFactory backgroundSubtractorFactory, IObjectDetectionGrpcClient objectDetectionClient,
    ILogger<CameraWorker> logger) : CameraWorkerBase(options, hubConnectionFactory, videoCaptureFactory, backgroundSubtractorFactory, objectDetectionClient)
{
    private readonly ILogger<CameraWorker> _logger = logger;
    private HubConnection? _remoteHubConnection;
    private HubConnection? _localHubConnection;
    private bool _remoteStreamingEnabled;

    public override async Task RunAsync(CancellationToken token)
    {
        try
        {
            await InitHubConnection(token);

            using var capture = await _videoCaptureFactory.CreateAsync(_options);
            using var subtractor = await _backgroundSubtractorFactory.CreateAsync(type: BackgroundSubtractorType.MOG2);
            using var foregroundMask = new Mat();

            while (!token.IsCancellationRequested)
            {
                using var capturedFrame = capture!.QueryFrame();
                if (capturedFrame == null || capturedFrame.IsEmpty) continue;

                var imageByteArray = ConvertFrameToByteArray(capturedFrame);

                if (ShouldRunInference(capturedFrame, foregroundMask, subtractor))
                {
                    imageByteArray = await RunInference(imageByteArray, token);
                }

                await SendFrameToHubAsync(imageByteArray, token);
            }

            await Task.WhenAll(
                new List<Task> {
                    StopAndDisposeLocalHubConnection(token),
                    StopAndDisposeRemoteHubConnection(token)
                });
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("CameraWorker for camera {cameraId} was cancelled.", CamId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred in CameraWorker for camera {cameraId}: {message}", CamId, ex.Message);
        }
        finally
        {
            _logger.LogInformation("CameraWorker for camera {cameraId} has stopped.", CamId);
        }
    }

    private bool ShouldRunInference(Mat capturedFrame, Mat fgMask, IBackgroundSubtractorAdapter subtractor)
    {
        switch (_options.Mode)
        {
            case InferenceMode.Continuous:
                return true;
            case InferenceMode.MotionBased:
                return MotionDetected(capturedFrame, fgMask, subtractor); // Handled separately in the main loop
            default:
                return false;
        }
    }

    public virtual async Task<byte[]> RunInference(byte[] frameData, CancellationToken cancellationToken = default)
    {
        // Call the gRPC object detection
        return await _objectDetectionClient.DetectObjectsAsync(frameData, cancellationToken);
    }

    /// <summary>
    /// Determines whether significant motion is detected in the current video frame.
    /// Optimized for CPU usage by downscaling and frame skipping.
    /// </summary>
    /// <param name="frame">The original captured video frame.</param>
    /// <param name="foregroundMask">A Mat to store the resulting foreground mask.</param>
    /// <param name="subtractor">The background subtractor instance.</param>
    /// <returns>True if motion is detected, otherwise false.</returns>
    protected virtual bool MotionDetected(Mat frame, Mat foregroundMask, IBackgroundSubtractorAdapter subtractor)
    {
        if (_frameCounter++ % _frameSkip != 0) return _lastMotionResult; // Only process every n-th frame, by frame skipping, to reduce CPU usage

        // Downscale the frame to reduce the number of pixels to process, improving performance.
        // Uses nearest neighbor interpolation for speed, which is sufficient for motion detection.
        frame.DownscaleTo(
            destination: _downscaledFrame,
            toWidth: _downscaledWidth,
            toHeight: _downscaledHeight,
            interpolationMethod: Inter.Nearest);

        // Alternative: Use pyramid downscaling for large reductions
        // _downscaledFrame = frame.PyramidDownscale(toWidth: _downscaledWidth, toHeight: _downscaledHeight);

        // Apply background subtraction to the downscaled frame.
        // The foregroundMask will contain white pixels where motion is detected.
        subtractor.Apply(_downscaledFrame, foregroundMask);

        // Count the number of non-zero (white) pixels in the mask, representing areas of motion.
        int motionPixels = CvInvoke.CountNonZero(foregroundMask);

        // Determine if the number of motion pixels exceeds the configured threshold.
        bool motionDetected = motionPixels > _motionThreshold;

        // Log only when the motion detection state changes (to reduce log noise).
        if (motionDetected != _lastMotionResult) _logger.LogInformation("Motion {status} with {pixels} pixels at {time}", motionDetected ? "detected" : "stopped", motionPixels, DateTime.Now);

        // Store the result for use in skipped frames and for change detection.
        _lastMotionResult = motionDetected;
        return motionDetected;
    }

    protected virtual byte[] ConvertFrameToByteArray(Mat frame, int quality = 78)
    {
        // Encode the Mat to JPEG directly into a byte array
        return frame.ToImage<Bgr, byte>().ToJpegData(quality);
    }

    // Note: MessagePack or json is a good alternative to send structured data, but not needed here since we only send byte[]
    protected virtual async Task SendFrameToHubAsync(byte[] imageByteArray, CancellationToken token)
    {
        // Via SignalR Cloud Hub
        if (_remoteStreamingEnabled)
        {
            //TODO: reduce image size when this is enabled (cost!)
            if (_remoteHubConnection != null && _remoteHubConnection.State == HubConnectionState.Connected)
                await _remoteHubConnection!.InvokeAsync("SendMessage", imageByteArray, $"{CamName}", token);
        }

        // Via SignalR Local Hub
        if (_localHubConnection != null && _localHubConnection.State == HubConnectionState.Connected)
            await _localHubConnection!.InvokeAsync("SendMessage", imageByteArray, $"{CamName}", token);
    }

    private async Task InitHubConnection(CancellationToken token)
    {
        //TODO: circuitbreaker for connection attempts
        _remoteHubConnection = _hubConnectionFactory.CreateConnection("https://localhost:7000/workerhub", apiKey: "123456789");
        _localHubConnection = _hubConnectionFactory.CreateConnection("https://localhost:7244/workerhub", apiKey: "123456789");

        try
        {
            await Task.WhenAll(
                new List<Task>
                {
                    _localHubConnection.StartAsync(token),
                    _localHubConnection.InvokeAsync("JoinGroup", CamName, token),
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing Local HubConnection for camera {cameraId}: {message}", CamId, ex.Message);
        }

        try
        {
            // Setup handlers for streaming control messages from the hub
            _remoteHubConnection.On("NotifyStreamingEnabled", () =>
            {
                _remoteStreamingEnabled = true;
                _logger.LogInformation("Stream enabled for camera {cameraId}", CamId);
            });

            _remoteHubConnection.On("NotifyStreamingDisabled", () =>
            {
                _remoteStreamingEnabled = false;
                _logger.LogInformation("Stream disabled for camera {cameraId}", CamId);
            });

            // Connect to the SignalR hub group for this camera
            await Task.WhenAll(
                new List<Task>
                {
                    _remoteHubConnection.StartAsync(token),
                    _remoteHubConnection.InvokeAsync("JoinGroup", CamName, token),
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing HubConnection for camera {cameraId}: {message}", CamId, ex.Message);
        }
    }

    private async Task StopAndDisposeRemoteHubConnection(CancellationToken token)
    {
        if (_remoteHubConnection != null)
        {
            try
            {
                await _remoteHubConnection.InvokeAsync("LeaveGroup", CamName, token);
                await _remoteHubConnection.StopAsync(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping HubConnection for camera {cameraId}: {message}", CamId, ex.Message);
                throw; // Rethrow to let the caller handle the failure
            }
            finally
            {
                await _remoteHubConnection.DisposeAsync();
                _remoteHubConnection = null;
            }
        }
    }

    private async Task StopAndDisposeLocalHubConnection(CancellationToken token)
    {
        if (_localHubConnection != null)
        {
            try
            {
                await _localHubConnection.InvokeAsync("LeaveGroup", CamName, token);
                await _localHubConnection.StopAsync(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping Local HubConnection for camera {cameraId}: {message}", CamId, ex.Message);
                throw; // Rethrow to let the caller handle the failure
            }
            finally
            {
                await _localHubConnection.DisposeAsync();
                _localHubConnection = null;
            }
        }
    }
}

public class WorkerProperties
{
    public required InferenceMode Mode { get; set; }
    public required CameraProperties CameraOptions { get; set; }
    public required MotionDetectionProperties MotionDetectionOptions { get; set; }
}

public class CameraProperties
{
    public required int Id { get; set; }
    public required string Name { get; set; }
    public required CameraResolutionProperties Resolution { get; set; }
    public required int Framerate { get; set; }
}

public class CameraResolutionProperties
{
    public required int Width { get; set; }
    public required int Height { get; set; }
}

public class MotionDetectionProperties
{
    public required int DownscaleFactor { get; set; } // Downscale by a factor of n
    public required double MotionRatio { get; set; } // n% of the area
}

public enum InferenceMode
{
    MotionBased,
    Continuous
}