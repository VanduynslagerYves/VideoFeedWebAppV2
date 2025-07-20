using System.Collections.Concurrent;

namespace CameraFeed.API.Video;
public interface ICameraWorkerManager
{
    Task<ICameraWorker> CreateCameraWorkerAsync(int cameraId);
    Task<int?> StartCameraWorkerAsync(int cameraId);
    Task<bool> StopCameraWorkerAsync(int cameraId);
    Task<ConcurrentDictionary<int, (ICameraWorker CameraWorker, CancellationTokenSource Cts, Task? Task)>> GetAvailableCameraWorkersAsync();
}

public class CameraWorkerManager(ILogger<CameraWorkerManager> logger, ICameraWorkerFactory cameraWorkerFactory) : ICameraWorkerManager
{
    private readonly ILogger<CameraWorkerManager> _logger = logger;
    private readonly ICameraWorkerFactory _cameraWorkerFactory = cameraWorkerFactory;

    private readonly ConcurrentDictionary<int, (ICameraWorker CameraWorker, CancellationTokenSource Cts, Task? Task)> _availableWorkers = new();

    public async Task<ICameraWorker> CreateCameraWorkerAsync(int cameraId)
    {
        var cts = new CancellationTokenSource();
        var cameraWorker = await _cameraWorkerFactory.CreateCameraWorkerAsync(cameraId); //pass token to factory, then to CameraWorker?

        _availableWorkers.TryAdd(cameraId, (cameraWorker, cts, null));

        var createdWorker = _availableWorkers[cameraId];
        return createdWorker.CameraWorker;
    }

    public async Task<ConcurrentDictionary<int, (ICameraWorker CameraWorker, CancellationTokenSource Cts, Task? Task)>> GetAvailableCameraWorkersAsync()
    {
        return await Task.FromResult(_availableWorkers);
    }

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
