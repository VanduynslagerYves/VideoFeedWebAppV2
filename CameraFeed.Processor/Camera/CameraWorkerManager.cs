using AutoMapper;
using CameraFeed.Processor.Camera.Factories;
using CameraFeed.Processor.DTOs;
using System.Collections.Concurrent;

namespace CameraFeed.Processor.Camera;

public interface ICameraWorkerManager
{
    Task<int> CreateWorkerAsync(WorkerProperties options);
    Task StartAsync(int cameraId);
    Task StopAsync(int cameraId);
    Task StopAllAsync();
    IEnumerable<CameraInfoDTO> GetWorkerDtos(bool isActive = true);
    IEnumerable<int> GetWorkerIds(bool isActive = true);
}

public class CameraWorkerManager(ICameraWorkerFactory cameraWorkerFactory, IMapper mapper) : ICameraWorkerManager
{
    private readonly ConcurrentDictionary<int, IWorkerHandle> _workersDict = new();

    //TODO: add action delegates here for OnCreate, OnStart, OnStop events
    //and invoke them in the respective methods
    //then subscribe to these events in the CameraWorkerStartupService or any other callers

    public async Task<int> CreateWorkerAsync(WorkerProperties options)
    {
        var camId = options.CameraOptions.Id;
        if (_workersDict.ContainsKey(camId)) return camId; //Or invoke OnAlreadyExists delegates

        var workerHandle = await cameraWorkerFactory.CreateAsync(options);
        _workersDict.TryAdd(camId, workerHandle);

        return camId;
    }

    public async Task StartAsync(int cameraId)
    {
        if (!_workersDict.TryGetValue(cameraId, out var workerHandle)) return; //Or invoke OnNotFound delegates
        await workerHandle.StartAsync();
    }

    public async Task StopAsync(int cameraId)
    {
        if (!_workersDict.TryRemove(cameraId, out var workerHandle)) return; //Or invoke OnNotFound delegates
        await workerHandle.StopAsync();
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
        return workers.Select(w => mapper.Map<CameraInfoDTO>(w));
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
