using CameraFeed.Processor.DTO;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;

namespace CameraFeed.Processor.Video;

public interface ICameraWorkerManager
{
    Task<IActionResult> StartCameraWorkerAsync(CameraWorkerOptions options);
    Task<bool> StopCameraWorkerAsync(int cameraId);
}

public class CameraWorkerManager(ICameraWorkerFactory cameraWorkerFactory, ILogger<CameraWorkerManager> logger) : ICameraWorkerManager
{
    // Injected as singleton
    private readonly ICameraWorkerFactory _cameraWorkerFactory = cameraWorkerFactory;
    private readonly ILogger<CameraWorkerManager> _logger = logger;
    private readonly ConcurrentDictionary<int, CameraWorkerEntry> _availableWorkers = new();

    public async Task<IActionResult> StartCameraWorkerAsync(CameraWorkerOptions options)
    {
        // Check if a worker for the given camera ID already exists in the dictionary.
        // If not, create and register a new camera worker entry.
        if (!_availableWorkers.TryGetValue(options.CameraId, out var workerTuple))
        {
            await CreateCameraWorkerAsync(options);
            workerTuple = _availableWorkers[options.CameraId];
        }

        // Attempt to start the worker and return an appropriate IActionResult.
        return await StartWorkerAsync(workerTuple);
    }

    private async Task CreateCameraWorkerAsync(CameraWorkerOptions options)
    {
        // Create a new cancellation token source for the camera worker.
        var cts = new CancellationTokenSource();

        // Create the camera worker instance using the factory.
        var cameraWorker = await _cameraWorkerFactory.CreateCameraWorkerAsync(options);

        // Register the new camera worker entry in the dictionary for future management.
        _availableWorkers.TryAdd(options.CameraId, new CameraWorkerEntry(cameraWorker, cts, null));
    }

    private async Task<IActionResult> StartWorkerAsync(CameraWorkerEntry cameraWorkerEntry)
    {
        var worker = cameraWorkerEntry.Worker;

        // If the worker is already running (i.e., RunningTask is not null), return a result indicating it's already started.
        if (cameraWorkerEntry.RunningTask != null)
            return CameraOperationResultFactory.Create(worker.CameraId, ResponseMessages.CameraAlreadyRunning);

        // Attempt to start the camera worker in the background.
        // StartCameraWorkerTaskAsync returns true if the worker was started successfully, false otherwise.
        var success = await StartCameraWorkerTaskAsync(cameraWorkerEntry);

        // Return an appropriate IActionResult based on whether the worker was started successfully.
        return success
            ? CameraOperationResultFactory.Create(worker.CameraId, ResponseMessages.CameraStarted)
            : CameraOperationResultFactory.Create(worker.CameraId, ResponseMessages.CameraStartFailed);
    }

    public async Task<bool> StartCameraWorkerTaskAsync(CameraWorkerEntry cameraWorkerEntry)
    {
        // Only start the worker if it is not already running
        if (cameraWorkerEntry.RunningTask == null)
        {
            var cameraId = cameraWorkerEntry.Worker.CameraId;
            try
            {
                _logger.LogInformation("Starting worker {id}", cameraId);

                // Start the camera worker in the background using Task.Run.
                // The delegate passed to Task.Run is asynchronous, so we must await it inside.
                // This ensures that any asynchronous operations within RunAsync are properly handled.
                var task = Task.Run(async () => await cameraWorkerEntry.Worker.RunAsync(cameraWorkerEntry.Cts.Token));
                _logger.LogInformation("Worker {id} is running...", cameraId);

                // Store the running task reference for later management (e.g., stopping, status checks)
                cameraWorkerEntry.RunningTask = task;
                _availableWorkers[cameraId] = cameraWorkerEntry;

                // Await Task.Yield() to ensure the method is truly asynchronous and does not block the calling thread.
                await Task.Yield();

                // If we reach here, starting the task succeeded
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start camera worker {id}", cameraId);

                // Remove the entry to keep the state clean
                _availableWorkers.TryRemove(cameraId, out _);

                return false;
            }
        }

        // If already running, consider it a failure to start a new one
        return false;
    }

    public async Task<bool> StopCameraWorkerAsync(int cameraId)
    {
        if (_availableWorkers.TryGetValue(cameraId, out var workerEntry) && workerEntry.RunningTask != null)
        {
            workerEntry.Cts.Cancel();
            await Task.Yield();

            return workerEntry.RunningTask.IsCanceled;
        }

        return true; // If the worker is not found or not running, consider it "stopped"
    }
}
public class CameraWorkerEntry(ICameraWorker worker, CancellationTokenSource cts, Task? runningTask)
{
    public ICameraWorker Worker { get; } = worker;
    public CancellationTokenSource Cts { get; } = cts;
    public Task? RunningTask { get; set; } = runningTask;
}
