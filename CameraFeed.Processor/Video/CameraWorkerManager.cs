using CameraFeed.Processor.DTO;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;

namespace CameraFeed.Processor.Video;

public interface IWorkerManager
{
    Task<IActionResult> StartAsync(StartWorkerOptions options);
    Task<IActionResult> StopAsync(StopWorkerOptions options);
}

public abstract class WorkerManagerBase : IWorkerManager
{
    public abstract Task<IActionResult> StartAsync(StartWorkerOptions options);
    public abstract Task<IActionResult> StopAsync(StopWorkerOptions options);
}

public class CameraWorkerManager(ICameraWorkerFactory cameraWorkerFactory, ILogger<CameraWorkerManager> logger) : WorkerManagerBase
{
    // Injected as singleton
    private readonly ICameraWorkerFactory _cameraWorkerFactory = cameraWorkerFactory;
    private readonly ILogger<CameraWorkerManager> _logger = logger;
    private readonly ConcurrentDictionary<int, WorkerEntry> _availableWorkers = new();

    private async Task<WorkerEntry> CreateCameraWorkerAsync(StartWorkerOptions options)
    {
        // Create a new cancellation token source for the camera worker.
        var cts = new CancellationTokenSource();

        // Create the camera worker instance using the factory.
        var cameraWorker = await _cameraWorkerFactory.CreateCameraWorkerAsync(options);

        var cameraWorkerEntry = new CameraWorkerEntry(cameraWorker, cts, null);
        // Register the new camera worker entry in the dictionary for future management.
        _availableWorkers.TryAdd(options.CameraId, cameraWorkerEntry);

        return cameraWorkerEntry;
    }

    public async override Task<IActionResult> StartAsync(StartWorkerOptions options)
    {
        // Check if a worker for the given camera ID already exists in the dictionary.
        // If not, create and register a new camera worker entry.
        if (!_availableWorkers.TryGetValue(options.CameraId, out var workerEntry))
        {
            workerEntry = await CreateCameraWorkerAsync(options);
        }

        // Attempt to start the worker and return an appropriate IActionResult.
        return await StartWorkerAsync(workerEntry);
    }

    private async Task<IActionResult> StartWorkerAsync(WorkerEntry cameraWorkerEntry)
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

    private async Task<bool> StartCameraWorkerTaskAsync(WorkerEntry workerEntry)
    {
        // Only start the worker if it is not already running
        if (workerEntry.RunningTask == null)
        {
            var cameraId = workerEntry.Worker.CameraId;
            try
            {
                _logger.LogInformation("Starting worker {id}", cameraId);
                workerEntry.Start();
                _logger.LogInformation("Worker {id} is running...", cameraId);

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

    public async override Task<IActionResult> StopAsync(StopWorkerOptions options)
    {
        try
        {
            if (_availableWorkers.TryGetValue(options.CameraId, out var workerEntry) && workerEntry.RunningTask != null)
            {
                workerEntry.Stop();
                await Task.Yield();
                return CameraOperationResultFactory.Create(options.CameraId, ResponseMessages.CameraStopped);
            }
            return CameraOperationResultFactory.Create(options.CameraId, ResponseMessages.CameraNotRunning);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping worker {CameraId}", options.CameraId);
            return CameraOperationResultFactory.Create(options.CameraId, "Error stopping camera worker.");
        }
    }
}

public abstract class WorkerEntry(ICameraWorker worker, CancellationTokenSource cts, Task? runningTask)
{
    public ICameraWorker Worker { get; } = worker;
    public CancellationTokenSource Cts { get; } = cts;
    public Task? RunningTask { get; set; } = runningTask;

    public abstract void Start();
    public abstract void Stop();
}

public class CameraWorkerEntry(ICameraWorker worker, CancellationTokenSource cts, Task? runningTask) : WorkerEntry(worker, cts, runningTask)
{
    public override void Start()
    {
        // Start the camera worker in the background using Task.Run.
        // The delegate passed to Task.Run is asynchronous, so we must await it inside.
        // This ensures that any asynchronous operations within RunAsync are properly handled.
        // Store the running task reference for later management (e.g., stopping, status checks)
        RunningTask ??= Task.Run(async () => await Worker.RunAsync(Cts.Token));
    }

    public override void Stop()
    {
        Cts.Cancel();
        Worker.ReleaseCapture();
        RunningTask = null;
    }
}

public abstract class WorkerOptions
{
    public required int CameraId { get; set; }
}

public class StopWorkerOptions : WorkerOptions { }

public class StartWorkerOptions : WorkerOptions
{
    public required bool UseContinuousInference { get; set; } = false;
    public required bool UseMotionDetection { get; set; } = false;
    public required CameraOptions CameraOptions { get; set; }
}
