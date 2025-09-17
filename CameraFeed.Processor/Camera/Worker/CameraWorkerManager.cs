using System.Collections.Concurrent;

namespace CameraFeed.Processor.Camera.Worker;

public interface ICameraWorkerManager
{
    Task<IWorkerEntry> CreateAsync(WorkerProperties options, CancellationToken cancellationToken);
    Task<IWorkerEntry> StartAsync(IWorkerEntry workerEntry);
    Task StopAllAsync();
    Task<List<IWorkerEntry>> GetActiveCameraWorkerEntries();
}

public class CameraWorkerManager(ICameraWorkerFactory cameraWorkerFactory, ILogger<CameraWorkerManager> logger) : ICameraWorkerManager
{
    private readonly ConcurrentDictionary<int, IWorkerEntry> _workers = new();

    public async Task<IWorkerEntry> CreateAsync(WorkerProperties options, CancellationToken cancellationToken)
    {
        if (_workers.TryGetValue(options.CameraOptions.Id, out var workerEntry)) return workerEntry;

        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var worker = await cameraWorkerFactory.CreateAsync(options);
        workerEntry = new CameraWorkerEntry(worker, cts, null);
        _workers.TryAdd(worker.CamId, workerEntry);

        return workerEntry;
    }

    public async Task<IWorkerEntry> StartAsync(IWorkerEntry workerEntry)
    {
        try
        {
            await workerEntry.StartAsync();
            logger.LogInformation("Worker for {id} is running...", workerEntry.Worker.CamName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start worker for {id}", workerEntry.Worker.CamName);
        }

        return workerEntry;
    }

    public async Task StopAllAsync()
    {
        foreach (var workerEntry in _workers.Values)
        {
            try
            {
                await workerEntry.StopAsync();
                logger.LogInformation("Worker for {id} has been stopped.", workerEntry.Worker.CamName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to stop worker for {id}", workerEntry.Worker.CamName);
            }
        }
    }

    public Task<List<IWorkerEntry>> GetActiveCameraWorkerEntries()
    {
        var activeWorkers = _workers.Values.Where(w => w.RunningTask != null).ToList();
        return Task.FromResult(activeWorkers);
    }
}
