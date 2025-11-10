using AutoMapper;
using CameraFeed.Processor.Camera;
using CameraFeed.Processor.Camera.Factories;
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
        var workerRepository = scope.ServiceProvider.GetRequiredService<IWorkerRepository>(); //TODO: refactor so we don't use ServiceLocator pattern

        var enabledWorkers = await workerRepository.GetEnabledWorkersAsync();
        if (enabledWorkers.Count == 0) return;

        // Initialize and start each enabled worker
        // Using Task.WhenAll to run startups in parallel

        //TODO: needs rework. If a worker is persisted but no longer connected, this will try to start it and fail for the other workers too
        var startupTasks = enabledWorkers.Select(async workerRecord =>
        {
            var options = _mapper.Map<WorkerProperties>(workerRecord);

            try
            {
                int workerId = await _workerManager.CreateWorkerAsync(options);
                await _workerManager.StartAsync(workerId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Worker {CameraId} failed to initialize and will not be started by CameraWorkerService.", workerRecord.CameraId);
            }
        });

        await Task.WhenAll(startupTasks);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _workerManager.StopAllAsync();
    }
}
