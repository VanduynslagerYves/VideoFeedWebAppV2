using CameraFeed.Processor.Clients.SignalR;

namespace CameraFeed.Processor.Camera.Factories;

public interface ICameraWorkerFactory
{
    Task<IWorkerHandle> CreateAsync(WorkerProperties options);
}

public class CameraWorkerFactory(ICameraSignalRclient signalRclient, IFrameProcessorFactory frameProcessorFactory, ILogger<CameraWorker> logger) : ICameraWorkerFactory
{
    public async Task<IWorkerHandle> CreateAsync(WorkerProperties options)
    {
        var frameProcessor = await frameProcessorFactory.CreateAsync(options);
        var worker =  new CameraWorker(options, signalRclient, frameProcessor, logger);
        var cts = new CancellationTokenSource();

        return new CameraWorkerHandle(worker, cts);
    }
}