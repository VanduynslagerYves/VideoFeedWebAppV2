using AutoMapper;
using CameraFeed.Processor.Camera.Worker;
using CameraFeed.Processor.Repositories;

namespace CameraFeed.Processor.BackgroundServices;

public interface ICameraWorkerStartupService
{
}

public abstract class CameraWorkerStartupServiceBase : ICameraWorkerStartupService
{
}

public class CameraWorkerStartupService(IServiceProvider serviceProvider, ICameraWorkerManager cameraInitializer, IMapper mapper, ILogger<CameraWorkerStartupService> logger) : CameraWorkerStartupServiceBase, IHostedService
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ILogger<CameraWorkerStartupService> _logger = logger;
    private readonly ICameraWorkerManager _workerManager = cameraInitializer;
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
        if (enabledWorkers.Count == 0) return;

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
            var options = _mapper.Map<WorkerProperties>(workerRecord);

            try
            {
                var workerEntry = await _workerManager.CreateAsync(options, cancellationToken);
                if(workerEntry.RunningTask == null) await _workerManager.StartAsync(workerEntry);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Worker {CameraId} failed to initialize and will not be started by CameraWorkerService.", workerRecord.CameraId);
            }
        });
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _workerManager.StopAllAsync();
    }
}
