using System.Collections.Concurrent;

namespace CameraAPI.Video;

public interface ICameraWorkerManager
{
    void CreatecCameraWorkers();
    int? StartCameraWorker(int cameraId);
    bool StopCameraWorker(int cameraId);
    HashSet<int> GetAvailableCameraWorkers();
}

public class CameraWorkerManager(ILogger<CameraWorkerManager> logger, ICameraWorkerFactory cameraWorkerFactory) : ICameraWorkerManager
{
    private readonly ILogger<CameraWorkerManager> _logger = logger;
    private readonly ICameraWorkerFactory _cameraWorkerFactory = cameraWorkerFactory;

    private readonly ConcurrentDictionary<int, (ICameraWorker CameraWorker, CancellationTokenSource Cts, Task? Task)> _workers = new();

    public void CreatecCameraWorkers()
    {
        var workers = new List<int> { 0, 1 };

        foreach(var workerId in workers)
        {
            var cts = new CancellationTokenSource();
            var cameraWorker = _cameraWorkerFactory.CreateCameraWorker(workerId);

            _workers.TryAdd(workerId, (cameraWorker, cts, null));
        }
    }

    public HashSet<int> GetAvailableCameraWorkers()
    {
        return [.. _workers.Keys];
    }

    public int? StartCameraWorker(int cameraId)
    {
        if(_workers.TryGetValue(cameraId, out var cameraWorker) && cameraWorker.Task == null)
        {
            var task = Task.Run(() => cameraWorker.CameraWorker.RunAsync(cameraWorker.Cts.Token));
            _workers[cameraId] = (cameraWorker.CameraWorker, cameraWorker.Cts, task);
            return task.Id;
        }

        return null;
    }

    public bool StopCameraWorker(int id)
    {
        if(_workers.TryRemove(id, out var cameraWorker))
        {
            cameraWorker.Cts.Cancel();
            _logger.LogInformation($"Stopping worker {id}");
            return true;
        }

        return false;
    }
}
