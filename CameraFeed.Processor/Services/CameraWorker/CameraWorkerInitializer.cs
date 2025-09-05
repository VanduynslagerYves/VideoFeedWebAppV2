using CameraFeed.Processor.Camera.Worker;

namespace CameraFeed.Processor.Services.CameraWorker;

public interface ICameraWorkerInitializer
{
    Task<WorkerEntry> CreateAndStartWorkerAsync(WorkerOptions options, CancellationToken cancellationToken);
}

public class CameraWorkerInitializer(ICameraWorkerFactory factory, ILogger<CameraWorkerInitializer> logger) : ICameraWorkerInitializer
{
    public async Task<WorkerEntry> CreateAndStartWorkerAsync(WorkerOptions options, CancellationToken cancellationToken)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var worker = await factory.CreateCameraWorkerAsync(options);
        var workerEntry = new CameraWorkerEntry(worker, cts, null);

        try
        {
            workerEntry.Start();
            logger.LogInformation("Worker {id} is running...", options.CameraId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start camera worker {id}", options.CameraId);
            cts.Dispose();
            throw;
        }

        return workerEntry;
    }
}
