using CameraFeed.Processor.Clients.SignalR;

namespace CameraFeed.Processor.Camera.Factories;

public interface ICameraWorkerFactory
{
    ICameraWorker Create(WorkerProperties options);
}

public class CameraWorkerFactory(ICameraSignalRclient signalRclient, IFrameProcessorFactory frameProcessorFactory, ILogger<CameraWorker> logger) : ICameraWorkerFactory
{
    public ICameraWorker Create(WorkerProperties options)
    {
        return new CameraWorker(options, signalRclient, frameProcessorFactory, logger);
    }
}