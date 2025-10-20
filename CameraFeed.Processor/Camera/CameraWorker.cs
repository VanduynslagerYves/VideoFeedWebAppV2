using CameraFeed.Processor.Camera.Factories;
using CameraFeed.Processor.Clients.SignalR;

namespace CameraFeed.Processor.Camera;

public interface ICameraWorker
{
    Task RunAsync(CancellationToken token);
    int CamId { get; }
    string CamName { get; }
    int CamWidth { get; }
    int CamHeight { get; }
}

public abstract class CameraWorkerBase(WorkerProperties options, ICameraSignalRclient cameraSignalRClient, IFrameProcessorFactory frameProcessorFactory) : ICameraWorker
{
    protected readonly ICameraSignalRclient _signalRclient = cameraSignalRClient;
    protected readonly IFrameProcessorFactory _frameProcessorFactory = frameProcessorFactory;
    protected readonly WorkerProperties _options = options;

    public int CamId { get; } = options.CameraOptions.Id;
    public string CamName { get; } = options.CameraOptions.Name;
    public int CamWidth { get; } = options.CameraOptions.Resolution.Width;
    public int CamHeight { get; } = options.CameraOptions.Resolution.Height;

    public abstract Task RunAsync(CancellationToken token);
}

public class CameraWorker(WorkerProperties options, ICameraSignalRclient signalRclient, IFrameProcessorFactory frameProcessorFactory, ILogger<CameraWorker> logger) : CameraWorkerBase(options, signalRclient, frameProcessorFactory)
{
    private readonly ILogger<CameraWorker> _logger = logger;

    public override async Task RunAsync(CancellationToken token)
    {
        try
        {
            //One SignalRclient per local and remote connection
            // if not connected, create and start connection
            //not injected, created per run, Create can stay, just need a Start method (StartAndJoinAsync)
            await _signalRclient.CreateConnectionsAsync(CamName, token);

            using var frameProcessor = await _frameProcessorFactory.CreateAsync(_options);

            while (!token.IsCancellationRequested)
            {
                var imageData = await frameProcessor.QueryAndProcessFrame(cancellationToken: token);

                var remoteStreamingEnabled = _signalRclient.IsRemoteStreamingEnabled(CamName);
                await _signalRclient.SendFrameToLocalAsync(imageData, CamName, token);
                if (remoteStreamingEnabled)
                {
                    await _signalRclient.SendFrameToRemoteAsync(imageData, CamName, token);
                }
            }

            await _signalRclient.StopAndDisposeConnectionsAsync(CamName, token); //TODO: check if this is necesarry, probably not since we pass the token to signalRClient
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