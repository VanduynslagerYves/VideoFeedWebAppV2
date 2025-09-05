using CameraFeed.Processor.Camera.Worker;

namespace CameraFeed.Processor.Services.CameraWorker;

public abstract class WorkerEntry(ICameraWorker worker, CancellationTokenSource cts, Task? runningTask)
{
    public ICameraWorker Worker { get; } = worker;
    public CancellationTokenSource Cts { get; } = cts;
    public Task? RunningTask { get; set; } = runningTask;

    public abstract void Start();
    public abstract void Stop();
}

public class CameraWorkerEntry(ICameraWorker worker, CancellationTokenSource cts, Task? runningTask) : WorkerEntry(worker, cts, runningTask)
{
    /// <summary>
    /// Starts the camera worker on a background thread.
    /// </summary>
    /// <remarks>This method initializes and runs the camera worker asynchronously using a background thread. 
    /// If the worker is already running, this method does nothing. The running task is stored for later management,
    /// such as stopping or checking the status.</remarks>
    public override void Start()
    {
        // The async delegate is handled correctly by Task.Run, so we do not await the call to RunAsync here. This avoids an extra state machine.
        // When you write an async method, the C# compiler automatically generates a state machine behind the scenes.
        // This state machine keeps track of where the method should resume after each await, allowing your code to pause and continue asynchronously without blocking the thread.
        RunningTask ??= Task.Run(() => Worker.RunAsync(Cts.Token));
    }

    public override void Stop()
    {
        Cts.Cancel();
        Cts.Dispose();
        RunningTask = null;
    }
}