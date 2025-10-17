using AutoMapper;
using CameraFeed.Shared.DTOs;
using System.Collections.Concurrent;

namespace CameraFeed.Processor.Camera.Worker;

public interface ICameraWorkerManager
{
    IWorkerHandle Create(WorkerProperties options, CancellationToken cancellationToken);
    Task<IWorkerHandle> StartAsync(IWorkerHandle workerEntry);
    Task StopAsync(int cameraId);
    Task StopAllAsync();
    IEnumerable<CameraInfoDTO> GetWorkerDtos(bool isActive = true);
    IEnumerable<int> GetWorkerIds(bool isActive = true);
}

public class CameraWorkerManager(ICameraWorkerFactory cameraWorkerFactory, IMapper mapper, ILogger<CameraWorkerManager> logger) : ICameraWorkerManager
{
    private readonly ConcurrentDictionary<int, IWorkerHandle> _workerHandles = new();
    private readonly ICameraWorkerFactory _cameraWorkerFactory = cameraWorkerFactory;
    private readonly IMapper _mapper = mapper;

    public IWorkerHandle Create(WorkerProperties options, CancellationToken cancellationToken)
    {
        if (_workerHandles.TryGetValue(options.CameraOptions.Id, out var workerHandle)) return workerHandle; // Return existing worker ID if already exists

        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var worker = _cameraWorkerFactory.Create(options);
        workerHandle = new CameraWorkerHandle(worker, cts, null);
        _workerHandles.TryAdd(worker.CamId, workerHandle); // Add the new worker entry to the dictionary

        return workerHandle; // Return the camera ID of the newly created worker
    }

    public async Task<IWorkerHandle> StartAsync(IWorkerHandle workerHandle)
    {
        try
        {
            await workerHandle.StartAsync();
            logger.LogInformation("Worker for {id} is running...", workerHandle.Worker.CamName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start worker for {id}", workerHandle.Worker.CamName);
        }

        return workerHandle;
    }

    public async Task StopAsync(int cameraId)
    {
        if (_workerHandles.TryRemove(cameraId, out var workerHandle))
        {
            try
            {
                await workerHandle.StopAsync();
                logger.LogInformation("Worker for {id} has been stopped and removed.", workerHandle.Worker.CamName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to stop worker for {id}", workerHandle.Worker.CamName);
            }
        }
        else
        {
            logger.LogWarning("No worker found for camera ID {id} to stop.", cameraId);
        }
    }

    public async Task StopAllAsync()
    {
        foreach (var workerHandle in _workerHandles.Values)
        {
            try
            {
                await workerHandle.StopAsync();
                logger.LogInformation("Worker for {id} has been stopped.", workerHandle.Worker.CamName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to stop worker for {id}", workerHandle.Worker.CamName);
            }
        }
    }

    public IEnumerable<CameraInfoDTO> GetWorkerDtos(bool isActive = true)
    {
        var workers = GetWorkers(isActive);
        return workers.Select(w => _mapper.Map<CameraInfoDTO>(w));
    }

    public IEnumerable<int> GetWorkerIds(bool isActive = true)
    {
        var workers = GetWorkers(isActive);
        return workers.Select(w => w.CamId);
    }

    private IEnumerable<ICameraWorker> GetWorkers(bool isActive = true)
    {
        return _workerHandles.Values
            .Where(w => isActive ? w.RunningTask != null : w.RunningTask == null)
            .Select(x => x.Worker);
    }
}
