using CameraFeed.Processor.Clients.SignalR;

namespace CameraFeed.Processor.Camera.Factories;

public interface ICameraWorkerFactory
{
    IWorkerHandle Create(WorkerProperties options);
}

public class CameraWorkerFactory(ICameraSignalRclient signalRclient, IFrameProcessorFactory frameProcessorFactory, ILogger<CameraWorker> logger) : ICameraWorkerFactory
{
    public IWorkerHandle Create(WorkerProperties options)
    {
        var worker =  new CameraWorker(options, signalRclient, frameProcessorFactory, logger);
        var cts = new CancellationTokenSource();

        return new CameraWorkerHandle(worker, cts);
    }
}