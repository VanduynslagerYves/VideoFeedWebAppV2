using System.Collections.Concurrent;

namespace CameraFeed.API.Video;

/// <summary>
/// Provides functionality to manage camera workers, allowing for creation, starting, stopping, and retrieval of camera
/// workers.
/// </summary>
/// <remarks>This interface is designed to handle multiple camera workers concurrently, providing asynchronous
/// methods to manage their lifecycle.</remarks>
public interface ICameraWorkerManager
{
    Task<ICameraWorker> CreateCameraWorkerAsync(int cameraId);
    Task<int?> StartCameraWorkerAsync(int cameraId);
    Task<bool> StopCameraWorkerAsync(int cameraId);
    Task<ConcurrentDictionary<int, (ICameraWorker CameraWorker, CancellationTokenSource Cts, Task? Task)>> GetAvailableCameraWorkersAsync();
}

/// <summary>
/// Manages the lifecycle and operations of camera workers, including creation, retrieval, starting, and stopping of
/// workers.
/// </summary>
/// <remarks>This class provides functionality to manage camera workers, which are responsible for handling
/// camera-related tasks. It maintains a collection of available workers and ensures proper resource management,
/// including cancellation and cleanup when workers are stopped.</remarks>
/// <param name="logger"></param>
/// <param name="cameraWorkerFactory"></param>
public class CameraWorkerManager(ICameraWorkerFactory cameraWorkerFactory, ILogger<CameraWorkerManager> logger) : ICameraWorkerManager
{
    private readonly ILogger<CameraWorkerManager> _logger = logger;
    private readonly ICameraWorkerFactory _cameraWorkerFactory = cameraWorkerFactory;

    private readonly ConcurrentDictionary<int, (ICameraWorker CameraWorker, CancellationTokenSource Cts, Task? Task)> _availableWorkers = new();

    /// <summary>
    /// Asynchronously creates and initializes a camera worker for the specified camera ID.
    /// </summary>
    /// <remarks>The created camera worker is added to the internal collection of available workers.</remarks>
    /// <param name="cameraId">The unique identifier of the camera for which the worker is to be created.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an instance of  <see
    /// cref="ICameraWorker"/> associated with the specified camera ID.</returns>
    public async Task<ICameraWorker> CreateCameraWorkerAsync(int cameraId)
    {
        var cts = new CancellationTokenSource();
        var cameraWorker = await _cameraWorkerFactory.CreateCameraWorkerAsync(cameraId); //pass token to factory, then to CameraWorker?

        _availableWorkers.TryAdd(cameraId, (cameraWorker, cts, null));

        var createdWorker = _availableWorkers[cameraId];
        return createdWorker.CameraWorker;
    }

    /// <summary>
    /// Retrieves a collection of available camera workers along with their associated cancellation tokens and tasks.
    /// </summary>
    /// <remarks>The returned dictionary contains information about each available camera worker, including
    /// its unique identifier, the worker instance, a cancellation token source for managing its lifecycle, and an
    /// optional task representing its current operation. The dictionary is thread-safe and can be accessed
    /// concurrently.</remarks>
    /// <returns>A <see cref="ConcurrentDictionary{TKey, TValue}"/> where the key is the unique identifier of the camera worker,
    /// and the value is a tuple containing the camera worker instance, its cancellation token source, and an optional
    /// task.</returns>
    public async Task<ConcurrentDictionary<int, (ICameraWorker CameraWorker, CancellationTokenSource Cts, Task? Task)>> GetAvailableCameraWorkersAsync()
    {
        return await Task.FromResult(_availableWorkers);
    }

    /// <summary>
    /// Starts the camera worker associated with the specified camera ID asynchronously.
    /// </summary>
    /// <remarks>This method attempts to start the camera worker for the specified camera ID. If the worker is
    /// already running or the camera ID does not exist in the available workers, the method returns <see
    /// langword="null"/>.</remarks>
    /// <param name="cameraId">The unique identifier of the camera whose worker should be started.</param>
    /// <returns>The ID of the task running the camera worker, or <see langword="null"/> if the worker is already running or the
    /// camera ID is invalid.</returns>
    public async Task<int?> StartCameraWorkerAsync(int cameraId)
    {
        if(_availableWorkers.TryGetValue(cameraId, out var cameraWorker) && cameraWorker.Task == null)
        {
            var task = Task.Run(() => cameraWorker.CameraWorker.RunAsync(cameraWorker.Cts.Token));
            _availableWorkers[cameraId] = (cameraWorker.CameraWorker, cameraWorker.Cts, task);

            await Task.Yield(); // Ensures async context is preserved
            return task.Id;
        }

        return null;
    }

    public async Task<bool> StopCameraWorkerAsync(int id)
    {
        if(_availableWorkers.TryRemove(id, out var cameraWorker))
        {
            cameraWorker.Cts.Cancel();
            _logger.LogInformation($"Stopping worker {id}");

            await Task.Yield(); // Ensures async context is preserved
            return true;
        }

        return false;
    }
}
