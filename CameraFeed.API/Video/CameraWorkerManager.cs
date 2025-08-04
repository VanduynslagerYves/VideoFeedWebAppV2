using Emgu.CV;
using System.Collections.Concurrent;

namespace CameraFeed.API.Video;

public interface ICameraWorkerManager
{
    Task<ICameraWorker> CreateCameraWorkerAsync(CameraWorkerOptions options);
    Task<int?> StartCameraWorkerAsync(int cameraId);
    Task<bool> StopCameraWorkerAsync(int cameraId);
    Task<ConcurrentDictionary<int, (ICameraWorker CameraWorker, CancellationTokenSource Cts, Task? Task)>> GetAvailableCameraWorkersAsync();
    Task<List<int>> GetAvailableCameraIds(int maxTested = 10);
}

public class CameraWorkerManager(ICameraWorkerFactory cameraWorkerFactory, ILogger<CameraWorkerManager> logger) : ICameraWorkerManager
{
    //Injected as singleton
    private readonly ICameraWorkerFactory _cameraWorkerFactory = cameraWorkerFactory;
    private readonly ILogger<CameraWorkerManager> _logger = logger;

    private readonly ConcurrentDictionary<int, (ICameraWorker CameraWorker, CancellationTokenSource Cts, Task? Task)> _availableWorkers = new();

    public async Task<ICameraWorker> CreateCameraWorkerAsync(CameraWorkerOptions options)
    {
        var cts = new CancellationTokenSource();
        var cameraId = options.CameraId;
        var cameraWorker = await _cameraWorkerFactory.CreateCameraWorkerAsync(options); //TODO: pass token to factory, then to CameraWorker?

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
            _logger.LogInformation("Starting worker {id}", cameraId);
            var task = Task.Run(() => cameraWorker.CameraWorker.RunAsync(cameraWorker.Cts.Token));
            _logger.LogInformation("Worker {id} is running...", cameraId);

            _availableWorkers[cameraId] = (cameraWorker.CameraWorker, cameraWorker.Cts, task);

            await Task.Yield(); // Ensures async context is preserved
            return task.Id;
        }

        return null;
    }

    public async Task<List<int>> GetAvailableCameraIds(int maxTested = 10)
    {
        var availableIds = new List<int>();
        for (int i = 0; i < maxTested; i++)
        {
            using var capture = new VideoCapture(i, VideoCapture.API.DShow); // DShow is common on Windows
            if (capture.IsOpened)
            {
                availableIds.Add(i);
            }
        }
        return await Task.FromResult(availableIds);
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
