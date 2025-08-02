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
    /// <summary>
    /// Asynchronously creates and initializes a new camera worker instance with the specified options.
    /// </summary>
    /// <remarks>The returned <see cref="ICameraWorker"/> instance is ready to use after the task completes.
    /// Ensure that the provided <paramref name="options"/> are valid and supported by the underlying camera
    /// system.</remarks>
    /// <param name="options">The configuration options for the camera worker, including settings such as resolution, frame rate, and other
    /// parameters. Cannot be null.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an <see cref="ICameraWorker"/>
    /// instance configured with the specified options.</returns>
    Task<ICameraWorker> CreateCameraWorkerAsync(CameraWorkerOptions options);
    /// <summary>
    /// Starts the camera worker asynchronously for the specified camera.
    /// </summary>
    /// <remarks>This method initializes and starts a background worker process for the specified camera. 
    /// Ensure that the camera ID is valid and that the system is in a state where the worker can be started.</remarks>
    /// <param name="cameraId">The unique identifier of the camera to start. Must be a valid and existing camera ID.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is the process ID of the camera worker if the
    /// operation succeeds; otherwise, <see langword="null"/> if the operation fails.</returns>
    Task<int?> StartCameraWorkerAsync(int cameraId);
    /// <summary>
    /// Stops the camera worker associated with the specified camera ID.
    /// </summary>
    /// <remarks>This method stops the background worker responsible for managing the specified camera. 
    /// Ensure that the camera ID provided corresponds to an active camera worker.</remarks>
    /// <param name="cameraId">The unique identifier of the camera whose worker should be stopped.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is <see langword="true"/> if the camera
    /// worker was successfully stopped;  otherwise, <see langword="false"/>.</returns>
    Task<bool> StopCameraWorkerAsync(int cameraId);
    /// <summary>
    /// Asynchronously retrieves a collection of available camera workers along with their associated metadata.
    /// </summary>
    /// <remarks>The returned dictionary maps unique integer identifiers to tuples containing the camera
    /// worker instance,  a cancellation token source for managing the worker's lifecycle, and an optional task
    /// representing the worker's current operation.</remarks>
    /// <returns>A task that represents the asynchronous operation. The task result is a  <see cref="ConcurrentDictionary{TKey,
    /// TValue}"/> where the key is an integer identifier, and the value is a tuple  containing the camera worker (<see
    /// cref="ICameraWorker"/>), a <see cref="CancellationTokenSource"/>, and an optional <see cref="Task"/>.</returns>
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
    //Injected as singleton
    private readonly ICameraWorkerFactory _cameraWorkerFactory = cameraWorkerFactory;
    private readonly ILogger<CameraWorkerManager> _logger = logger;

    private readonly ConcurrentDictionary<int, (ICameraWorker CameraWorker, CancellationTokenSource Cts, Task? Task)> _availableWorkers = new();

    /// <summary>
    /// Asynchronously creates and initializes a camera worker for the specified camera ID.
    /// </summary>
    /// <remarks>The created camera worker is added to the internal collection of available workers.</remarks>
    /// <param name="cameraId">The unique identifier of the camera for which the worker is to be created.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an instance of  <see
    /// cref="ICameraWorker"/> associated with the specified camera ID.</returns>
    public async Task<ICameraWorker> CreateCameraWorkerAsync(CameraWorkerOptions options)
    {
        var cts = new CancellationTokenSource();
        var cameraId = options.CameraId;
        var cameraWorker = await _cameraWorkerFactory.CreateCameraWorkerAsync(options); //TODO: pass token to factory, then to CameraWorker?

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
            _logger.LogInformation("Starting worker {id}", cameraId);
            var task = Task.Run(() => cameraWorker.CameraWorker.RunAsync(cameraWorker.Cts.Token));
            _logger.LogInformation("Worker {id} is running...", cameraId);

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
            _logger.LogInformation("Stopping worker {id}", id);

            await Task.Yield(); // Ensures async context is preserved
            return true;
        }

        return false;
    }
}
