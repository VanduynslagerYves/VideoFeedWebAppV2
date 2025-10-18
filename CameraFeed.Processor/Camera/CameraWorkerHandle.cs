namespace CameraFeed.Processor.Camera;

public interface IWorkerHandle
{
    ICameraWorker Worker { get; }
    CancellationTokenSource Cts { get; }
    Task? RunningTask { get; set; }
    Task StartAsync();
    Task StopAsync();
}

public abstract class WorkerHandle(ICameraWorker worker, CancellationTokenSource cts, Task? runningTask) : IWorkerHandle
{
    public ICameraWorker Worker { get; } = worker;
    public CancellationTokenSource Cts { get; } = cts;
    public Task? RunningTask { get; set; } = runningTask;

    public abstract Task StartAsync();
    public abstract Task StopAsync();
}

public class CameraWorkerHandle(ICameraWorker worker, CancellationTokenSource cts, Task? runningTask) : WorkerHandle(worker, cts, runningTask)
{
    /// <summary>
    /// Starts the camera worker on a background thread.
    /// </summary>
    /// <remarks>This method initializes and runs the camera worker using a background thread. 
    /// If the worker is already running, this method does nothing. The running task is stored for later management,
    /// such as stopping or checking the status.</remarks>
    public override Task StartAsync()
    {
        // The async delegate is handled correctly by Task.Run, so we do not await the call to RunAsync here. This avoids an extra state machine.
        // When you write an async method, the C# compiler automatically generates a state machine behind the scenes.
        // This state machine keeps track of where the method should resume after each await, allowing your code to pause and continue asynchronously without blocking the thread.

        //Run on the thread pool for CPU heavey work
        //RunningTask ??= Task.Run(() => Worker.RunAsync(Cts.Token));

        //No need to put this on the thread pool, since it is just I/O
        RunningTask ??= Worker.RunAsync(Cts.Token);
        return Task.CompletedTask;
    }

    public override Task StopAsync()
    {
        Cts.Cancel();
        Cts.Dispose();
        RunningTask = null;
        return Task.CompletedTask;
    }
}