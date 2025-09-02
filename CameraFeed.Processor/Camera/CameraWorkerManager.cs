using CameraFeed.Processor.Camera.Worker;
using CameraFeed.Processor.DTO;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;

namespace CameraFeed.Processor.Camera;

public interface IWorkerManager
{
    Task<IActionResult> StartAsync(WorkerOptions options);
    Task<IActionResult> StopAsync(int cameraId);
}

public abstract class WorkerManagerBase : IWorkerManager
{
    public abstract Task<IActionResult> StartAsync(WorkerOptions options);
    public abstract Task<IActionResult> StopAsync(int cameraId);
}

public class CameraWorkerManager(ICameraWorkerFactory cameraWorkerFactory, ILogger<CameraWorkerManager> logger) : WorkerManagerBase
{
    // Injected as singleton
    private readonly ICameraWorkerFactory _cameraWorkerFactory = cameraWorkerFactory;
    private readonly ILogger<CameraWorkerManager> _logger = logger;
    private readonly ConcurrentDictionary<int, WorkerEntry> _availableWorkers = new();

    public async override Task<IActionResult> StartAsync(WorkerOptions options)
    {
        // Check if a worker for the given camera ID already exists in the dictionary.
        // If not, create and register a new camera worker entry.
        if (!_availableWorkers.TryGetValue(options.CameraId, out var workerEntry))
        {
            workerEntry = await CreateCameraWorkerAsync(options);
        }

        // Attempt to start the worker and return an appropriate IActionResult.
        return await StartCameraWorkerAsync(workerEntry);
    }

    private async Task<WorkerEntry> CreateCameraWorkerAsync(WorkerOptions options)
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

    private async Task<IActionResult> StartCameraWorkerAsync(WorkerEntry cameraWorkerEntry)
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

    private async Task<bool> StartCameraWorkerTaskAsync(WorkerEntry cameraWorkerEntry)
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

    public async override Task<IActionResult> StopAsync(int cameraId)
    {
        try
        {
            if (_availableWorkers.TryGetValue(cameraId, out var workerEntry) && workerEntry.RunningTask != null)
            {
                workerEntry.Stop();
                await Task.Yield();
                return CameraOperationResultFactory.Create(cameraId, ResponseMessages.CameraStopped);
            }
            return CameraOperationResultFactory.Create(cameraId, ResponseMessages.CameraNotRunning);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping worker {CameraId}", cameraId);
            return CameraOperationResultFactory.Create(cameraId, "Error stopping camera worker.");
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
    /// <summary>
    /// Starts the camera worker on a background thread.
    /// </summary>
    /// <remarks>This method initializes and runs the camera worker asynchronously using a background thread. 
    /// If the worker is already running, this method does nothing. The running task is stored for later management,
    /// such as stopping or checking the status.</remarks>
    public override void Start()
    {
        // The async delegate is handled correctly by Task.Run, so we do not await the call to RunAsync here. This avoids an extra state machine.
        // When you write an async method, the C# compiler automatically generates a state machine behind the scenes.
        // This state machine keeps track of where the method should resume after each await, allowing your code to pause and continue asynchronously without blocking the thread.
        RunningTask ??= Task.Run(() => Worker.RunAsync(Cts.Token));
    }

    public override void Stop()
    {
        Cts.Cancel();
        RunningTask = null;
    }
}
