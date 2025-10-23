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

public abstract class CameraWorkerBase(WorkerProperties options) : ICameraWorker
{
    protected readonly WorkerProperties _options = options;

    public int CamId { get; } = options.CameraOptions.Id;
    public string CamName { get; } = options.CameraOptions.Name;
    public int CamWidth { get; } = options.CameraOptions.Resolution.Width;
    public int CamHeight { get; } = options.CameraOptions.Resolution.Height;

    public abstract Task RunAsync(CancellationToken token);
}

public class CameraWorker(WorkerProperties options, ICameraSignalRclient signalRclient, IFrameProcessor frameProcessor, ILogger<CameraWorker> logger) : CameraWorkerBase(options)
{
    private readonly ILogger<CameraWorker> _logger = logger;

    public override async Task RunAsync(CancellationToken token)
    {
        _logger.LogInformation("Worker for {camName} has started.", CamName);

        try
        {
            //One SignalRclient per local and remote connection
            // if not connected, create and start connection
            //not injected, created per run
            await signalRclient.CreateConnectionsAsync(CamName, token);

            while (!token.IsCancellationRequested)
            {
                var imageData = await frameProcessor.QueryAndProcessFrame(cancellationToken: token);

                var remoteStreamingEnabled = signalRclient.IsRemoteStreamingEnabled(CamName);
                await signalRclient.SendFrameToLocalAsync(imageData, CamName, token);
                if (remoteStreamingEnabled)
                {
                    await signalRclient.SendFrameToRemoteAsync(imageData, CamName, token);
                }
            }
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
            await signalRclient.StopAndDisposeConnectionsAsync(CamName, token); //TODO: check if this is necesarry, probably not since we pass the token to signalRClient
            frameProcessor.Dispose();
            _logger.LogInformation("Worker for {camName} is stopped.", CamName);
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