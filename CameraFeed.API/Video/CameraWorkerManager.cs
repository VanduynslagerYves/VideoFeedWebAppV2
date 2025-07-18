using System.Collections.Concurrent;

namespace CameraFeed.API.Video;
public interface ICameraWorkerManager
{
    Task CreatecCameraWorkersAsync();
    Task<int?> StartCameraWorkerAsync(int cameraId);
    Task<bool> StopCameraWorkerAsync(int cameraId);
    Task<HashSet<int>> GetAvailableCameraWorkersAsync();
}

public class CameraWorkerManager(ILogger<CameraWorkerManager> logger, ICameraWorkerFactory cameraWorkerFactory) : ICameraWorkerManager
{
    private readonly ILogger<CameraWorkerManager> _logger = logger;
    private readonly ICameraWorkerFactory _cameraWorkerFactory = cameraWorkerFactory;

    private readonly ConcurrentDictionary<int, (ICameraWorker CameraWorker, CancellationTokenSource Cts, Task? Task)> _workers = new();

    public async Task CreatecCameraWorkersAsync()
    {
        var workers = new List<int> { 0, 1 };

        foreach(var workerId in workers)
        {
            var cts = new CancellationTokenSource();
            var cameraWorker = await _cameraWorkerFactory.CreateCameraWorkerAsync(workerId);

            _workers.TryAdd(workerId, (cameraWorker, cts, null));
        }

        await Task.CompletedTask;
    }

    public async Task<HashSet<int>> GetAvailableCameraWorkersAsync()
    {
        return await Task.FromResult<HashSet<int>>([.. _workers.Keys]);
    }

    public async Task<int?> StartCameraWorkerAsync(int cameraId)
    {
        if(_workers.TryGetValue(cameraId, out var cameraWorker) && cameraWorker.Task == null)
        {
            var task = Task.Run(() => cameraWorker.CameraWorker.RunAsync(cameraWorker.Cts.Token));
            _workers[cameraId] = (cameraWorker.CameraWorker, cameraWorker.Cts, task);

            await Task.Yield(); // Ensures async context is preserved
            return task.Id;
        }

        return null;
    }

    public async Task<bool> StopCameraWorkerAsync(int id)
    {
        if(_workers.TryRemove(id, out var cameraWorker))
        {
            cameraWorker.Cts.Cancel();
            _logger.LogInformation($"Stopping worker {id}");

            await Task.Yield(); // Ensures async context is preserved
            return true;
        }

        return false;
    }
}
