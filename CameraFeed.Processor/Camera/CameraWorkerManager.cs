using AutoMapper;
using CameraFeed.Processor.Camera.Factories;
using CameraFeed.Shared.DTOs;
using System.Collections.Concurrent;

namespace CameraFeed.Processor.Camera;

public interface ICameraWorkerManager
{
    int CreateWorker(WorkerProperties options);
    Task StartAsync(int cameraId);
    Task StopAsync(int cameraId);
    Task StopAllAsync();
    IEnumerable<CameraInfoDTO> GetWorkerDtos(bool isActive = true);
    IEnumerable<int> GetWorkerIds(bool isActive = true);
}

public class CameraWorkerManager(ICameraWorkerFactory cameraWorkerFactory, IMapper mapper, ILogger<CameraWorkerManager> logger) : ICameraWorkerManager
{
    private readonly ConcurrentDictionary<int, IWorkerHandle> _workersDict = new();
    private readonly ICameraWorkerFactory _cameraWorkerFactory = cameraWorkerFactory;
    private readonly IMapper _mapper = mapper;

    //TODO: add action delegates here for OnCreate, OnStart, OnStop events
    //and invoke them in the respective methods
    //then subscribe to these events in the CameraWorkerStartupService or any other callers

    public int CreateWorker(WorkerProperties options)
    {
        var camId = options.CameraOptions.Id;
        if (_workersDict.ContainsKey(camId)) return camId; //Or invoke OnAlreadyExists delegates

        var workerHandle = _cameraWorkerFactory.Create(options);
        _workersDict.TryAdd(camId, workerHandle);

        return camId;
    }

    public async Task StartAsync(int cameraId)
    {
        if (!_workersDict.TryGetValue(cameraId, out var workerHandle)) return; //Or invoke OnNotFound delegates

        try
        {
            await workerHandle.StartAsync();
            logger.LogInformation("Worker for {camName} has started.", workerHandle.Worker.CamName);
            //Invoke here any OnStart delegates if implemented
        }
        catch (Exception ex)
        {
            //Invoke here any OnError delegates if implemented
            logger.LogError(ex, "Worker for {camName} failed to start.", workerHandle.Worker.CamName);
        }
    }

    public async Task StopAsync(int cameraId)
    {
        if (!_workersDict.TryRemove(cameraId, out var workerHandle)) return; //Or invoke OnNotFound delegates

        try
        {
            await workerHandle.StopAsync();
            logger.LogInformation("Worker for {camName} is stopped.", workerHandle.Worker.CamName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Worker for {camName} failed to stop.", workerHandle.Worker.CamName);
        }
    }

    public async Task StopAllAsync()
    {
        foreach (var workerHandle in _workersDict.Values)
        {
            await StopAsync(workerHandle.Worker.CamId);
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
        return _workersDict.Values
            .Where(w => isActive ? w.RunningTask != null : w.RunningTask == null)
            .Select(x => x.Worker);
    }
}
