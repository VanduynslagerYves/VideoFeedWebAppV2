using AutoMapper;
using CameraFeed.Processor.Camera.Worker;
using CameraFeed.Processor.Repositories;
using System.Collections.Concurrent;

namespace CameraFeed.Processor.Services.CameraWorker;

public interface IWorkerService
{
    Task<List<int>> GetActiveCameraIdsAsync();
    Task<List<int>> GetAvailableCameraIdsAsync();
}

public abstract class WorkerServiceBase : IWorkerService
{
    public abstract Task<List<int>> GetActiveCameraIdsAsync();
    public abstract Task<List<int>> GetAvailableCameraIdsAsync();
}

public class CameraWorkerService(IServiceProvider serviceProvider, ICameraWorkerInitializer cameraInitializer, IMapper mapper, ILogger<CameraWorkerService> logger) : WorkerServiceBase, IHostedService
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ILogger<CameraWorkerService> _logger = logger;
    private readonly ICameraWorkerInitializer _initializer = cameraInitializer;
    private readonly ConcurrentDictionary<int, WorkerEntry> _workers = new();
    private readonly IMapper _mapper = mapper;

    /// <summary>
    /// Starts the asynchronous initialization and execution of camera workers based on the enabled worker
    /// configurations. This is used to automatically start the workers after a service restart.
    /// </summary>
    /// <remarks>This method retrieves the list of enabled workers from the database and initializes camera
    /// workers for each configuration.</remarks>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var factory = scope.ServiceProvider.GetRequiredService<ICameraWorkerFactory>();
        var workerRepository = scope.ServiceProvider.GetRequiredService<IWorkerRepository>();

        var enabledWorkers = await workerRepository.GetEnabledWorkersAsync();

        // Note:
        // Using ParallelOptions with MaxDegreeOfParallelism limits the number of concurrent operations.
        // This helps prevent resource exhaustion and balances system load, especially when initializing many workers.
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = 4,
            CancellationToken = cancellationToken
        };

        // Note:
        // Parallel.ForEachAsync allows multiple workers to be initialized concurrently.
        // This significantly improves startup performance compared to sequential looping,
        // as it reduces total initialization time and makes better use of available system resources.
        await Parallel.ForEachAsync(enabledWorkers, parallelOptions, async (workerRecord, ct) =>
        {
            var options = _mapper.Map<WorkerOptions>(workerRecord);
            if (!_workers.ContainsKey(workerRecord.CameraId))
            {
                try
                {
                    var workerEntry = await _initializer.CreateAndStartWorkerAsync(options, cancellationToken);
                    _workers.TryAdd(workerRecord.CameraId, workerEntry); //TODO: save to db instead of in-memory dictionary
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Worker {CameraId} failed to initialize and will not be started by CameraWorkerService.", workerRecord.CameraId);
                }
            }
        });
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var workerEntry in _workers.Values)
        {
            workerEntry.Stop();
        }
        return Task.CompletedTask;
    }

    //TODO: refactor so we get the list of active camera ids from the repository instead of the in-memory dictionary
    public override Task<List<int>> GetActiveCameraIdsAsync()
    {
        var activeCameraIds = _workers
            .Where(kvp => kvp.Value.RunningTask != null && !kvp.Value.RunningTask.IsCompleted)
            .Select(kvp => kvp.Key)
            .ToList();

        return Task.FromResult(activeCameraIds);
    }

    public override Task<List<int>> GetAvailableCameraIdsAsync()
    {
        var activeCameraIds = _workers.Select(x => x.Key).ToList();
        return Task.FromResult(activeCameraIds);
    }
}
