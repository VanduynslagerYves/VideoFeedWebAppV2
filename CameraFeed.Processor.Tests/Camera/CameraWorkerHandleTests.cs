using CameraFeed.Processor.Camera;
using Moq;

namespace CameraFeed.Processor.Tests.Camera;

public class CameraWorkerHandleTests
{
    [Fact]
    public async Task StartAsync_ShouldSetRunningTaskAndCallWorkerRunAsync()
    {
        var workerMock = new Mock<ICameraWorker>();
        var cts = new CancellationTokenSource();
        var runAsyncCalled = false;

        workerMock.Setup(w => w.RunAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback(() => runAsyncCalled = true);

        var handle = new CameraWorkerHandle(workerMock.Object, cts);

        await handle.StartAsync();

        Assert.NotNull(handle.RunningTask);
        Assert.True(runAsyncCalled);
    }

    [Fact]
    public async Task StartAsync_ShouldNotOverwriteExistingRunningTask()
    {
        var workerMock = new Mock<ICameraWorker>();
        var cts = new CancellationTokenSource();
        var existingTask = Task.CompletedTask;

        var handle = new CameraWorkerHandle(workerMock.Object, cts)
        {
            RunningTask = existingTask
        };

        await handle.StartAsync();

        Assert.Same(existingTask, handle.RunningTask);
        workerMock.Verify(w => w.RunAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task StopAsync_ShouldCancelAndDisposeTokenAndSetRunningTaskToNull()
    {
        var workerMock = new Mock<ICameraWorker>();
        var cts = new CancellationTokenSource();
        var handle = new CameraWorkerHandle(workerMock.Object, cts)
        {
            RunningTask = Task.CompletedTask
        };

        await handle.StopAsync();

        Assert.Null(handle.RunningTask);
        Assert.True(cts.IsCancellationRequested);
    }

    [Fact]
    public async Task StopAsync_ShouldNotThrowIfAlreadyDisposed()
    {
        var workerMock = new Mock<ICameraWorker>();
        var cts = new CancellationTokenSource();
        cts.Dispose();

        var handle = new CameraWorkerHandle(workerMock.Object, cts)
        {
            RunningTask = Task.CompletedTask
        };

        var ex = await Record.ExceptionAsync(() => handle.StopAsync());
        Assert.Null(ex);
        Assert.Null(handle.RunningTask);
    }
}