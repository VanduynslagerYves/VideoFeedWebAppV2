using CameraFeed.Processor.Camera;
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

public class CameraWorkerService(IServiceProvider serviceProvider, ICameraWorkerInitializer cameraInitializer, ILogger<CameraWorkerService> logger) : WorkerServiceBase, IHostedService
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ILogger<CameraWorkerService> _logger = logger;
    private readonly ICameraWorkerInitializer _initializer = cameraInitializer;
    private readonly ConcurrentDictionary<int, WorkerEntry> _availableWorkers = new();

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var factory = scope.ServiceProvider.GetRequiredService<ICameraWorkerFactory>();
        var cameraRepository = scope.ServiceProvider.GetRequiredService<ICameraRepository>(); //Should be workerRepository but it's not implemented yet.

        var cameraIds = new[] { 0, 1 }; //TODO: get from config or database (for state restore after service restart)
        //add and remove cameras can still happen through regular http calls.

        foreach (var id in cameraIds)
        {
            var options = new WorkerOptions
            {
                CameraId = id,
                CameraName = $"Camera {id}",
                Mode = InferenceMode.MotionBased,
                CameraOptions = new CameraOptions
                {
                    Resolution = SupportedCameraProperties.GetResolutionById("720p"),
                    Framerate = 15,
                },
                MotionDetectionOptions = new MotionDetectionOptions
                {
                    DownscaleFactor = 16,
                    MotionRatio = 0.005,
                }
            };

            if (!_availableWorkers.ContainsKey(id))
            {
                try
                {
                    var workerEntry = await _initializer.CreateAndStartWorkerAsync(options, cancellationToken);
                    _availableWorkers.TryAdd(id, workerEntry);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Worker {CameraId} failed to initialize and will not be started by CameraWorkerService.", id);
                }
            }
        }
    }

    [Obsolete("This method is deprecated and will be removed in a later version")]
    private async Task StartCameraWorkerAsync(WorkerEntry cameraWorkerEntry)
    {
        // If the worker is already running (i.e., RunningTask is not null), return a result indicating it's already started.
        if (cameraWorkerEntry.RunningTask != null) return;
        await StartCameraWorkerTaskAsync(cameraWorkerEntry);
    }

    [Obsolete("This method is deprecated and will be removed in a later version")]
    private Task StartCameraWorkerTaskAsync(WorkerEntry cameraWorkerEntry)
    {
        // Only start the worker if it is not already running
        if (cameraWorkerEntry.RunningTask == null)
        {
            var cameraId = cameraWorkerEntry.Worker.CameraId;
            try
            {
                _logger.LogInformation("Starting worker {id}", cameraId);

                cameraWorkerEntry.Start();

                _logger.LogInformation("Worker {id} is running...", cameraId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start camera worker {id}", cameraId);

                // Remove the entry to keep the state clean
                _availableWorkers.TryRemove(cameraId, out _);
            }
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var workerEntry in _availableWorkers.Values)
        {
            workerEntry.Stop();
        }
        return Task.CompletedTask;
    }

    public override Task<List<int>> GetActiveCameraIdsAsync()
    {
        var activeCameraIds = _availableWorkers
            .Where(kvp => kvp.Value.RunningTask != null && !kvp.Value.RunningTask.IsCompleted)
            .Select(kvp => kvp.Key)
            .ToList();

        return Task.FromResult(activeCameraIds);
    }

    public override Task<List<int>> GetAvailableCameraIdsAsync()
    {
        var activeCameraIds = _availableWorkers.Select(x => x.Key).ToList();
        return Task.FromResult(activeCameraIds);
    }
}
