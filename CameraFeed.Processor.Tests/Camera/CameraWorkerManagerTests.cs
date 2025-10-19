using AutoMapper;
using CameraFeed.Processor.Camera;
using CameraFeed.Processor.Camera.Factories;
using CameraFeed.Shared.DTOs;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Concurrent;

namespace CameraFeed.Processor.Tests.Camera;

public class CameraWorkerManagerTests
{
    private readonly Mock<ICameraWorkerFactory> _workerFactoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILogger<CameraWorkerManager>> _loggerMock = new();

    private readonly Mock<IWorkerHandle> _workerHandleMock = new();
    private readonly Mock<ICameraWorker> _cameraWorkerMock = new();

    private readonly WorkerProperties _workerProps = new()
    {
        Mode = InferenceMode.Continuous,
        CameraOptions = new CameraProperties
        {
            Id = 1,
            Name = "TestCam",
            Resolution = new CameraResolutionProperties
            {
                Width = 640,
                Height = 480
            },
            Framerate = 30
        },
        MotionDetectionOptions = new MotionDetectionProperties
        {
            DownscaleFactor = 2,
            MotionRatio = 0.05
        }
    };

    public CameraWorkerManagerTests()
    {
        _cameraWorkerMock.SetupGet(w => w.CamId).Returns(1);
        _cameraWorkerMock.SetupGet(w => w.CamName).Returns("TestCam");
        _cameraWorkerMock.SetupGet(w => w.CamWidth).Returns(640);
        _cameraWorkerMock.SetupGet(w => w.CamHeight).Returns(480);

        _workerHandleMock.SetupGet(h => h.Worker).Returns(_cameraWorkerMock.Object);
        _workerHandleMock.SetupProperty(h => h.RunningTask);

        _workerFactoryMock.Setup(f => f.Create(It.IsAny<WorkerProperties>())).Returns(_cameraWorkerMock.Object);
    }

    private CameraWorkerManager CreateManager() => new(_workerFactoryMock.Object, _mapperMock.Object, _loggerMock.Object);

    [Fact]
    public void Create_ShouldAddAndReturnWorkerHandle()
    {
        var manager = CreateManager();
        var cts = new CancellationTokenSource();

        var handle = manager.Create(_workerProps, cts.Token);

        Assert.NotNull(handle);
        Assert.Equal(_cameraWorkerMock.Object, handle.Worker);

        // Creating again should return the same handle (not a new one)
        var handle2 = manager.Create(_workerProps, cts.Token);
        Assert.Same(handle, handle2);
    }

    [Fact]
    public async Task StartAsync_ShouldCallStartAndLog()
    {
        var manager = CreateManager();
        _workerHandleMock.Setup(h => h.StartAsync()).Returns(Task.CompletedTask).Verifiable();

        var result = await manager.StartAsync(_workerHandleMock.Object);

        _workerHandleMock.Verify(h => h.StartAsync(), Times.Once);
        Assert.Same(_workerHandleMock.Object, result);
    }

    [Fact]
    public async Task StopAsync_ShouldRemoveAndStopWorker()
    {
        var manager = CreateManager();
        var cts = new CancellationTokenSource();

        // Replace the handle in the dictionary with the mock
        var workerHandlesField = typeof(CameraWorkerManager)
            .GetField("_workerHandles", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var workerHandles = (ConcurrentDictionary<int, IWorkerHandle>)workerHandlesField!.GetValue(manager)!;
        workerHandles[1] = _workerHandleMock.Object;

        // Setup the mock
        _workerHandleMock.Setup(h => h.StopAsync()).Returns(Task.CompletedTask).Verifiable();

        await manager.StopAsync(1);

        _workerHandleMock.Verify(h => h.StopAsync(), Times.Once);
    }

    [Fact]
    public async Task StopAsync_ShouldLogWarningIfWorkerNotFound()
    {
        var manager = CreateManager();
        // No worker created for id 99
        await manager.StopAsync(99);

        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No worker found for camera ID")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StopAllAsync_ShouldStopAllWorkers()
    {
        var manager = CreateManager();
        var cts = new CancellationTokenSource();

        // Replace the handle in the dictionary with the mock
        var workerHandlesField = typeof(CameraWorkerManager)
            .GetField("_workerHandles", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var workerHandles = (ConcurrentDictionary<int, IWorkerHandle>)workerHandlesField!.GetValue(manager)!;
        workerHandles[1] = _workerHandleMock.Object;
        workerHandles[2] = _workerHandleMock.Object;

        // Setup the mock
        _workerHandleMock.Setup(h => h.StopAsync()).Returns(Task.CompletedTask).Verifiable();

        await manager.StopAllAsync();

        _workerHandleMock.Verify(h => h.StopAsync(), Times.Exactly(2));
    }

    [Fact]
    public void GetWorkerDtos_ShouldReturnMappedDtos()
    {
        var manager = CreateManager();
        var cts = new CancellationTokenSource();
        var handle = manager.Create(_workerProps, cts.Token);

        // Mark the worker as active
        handle.RunningTask = Task.CompletedTask;

        var dto = new CameraInfoDTO { Id = 1, Name = "TestCam", Width = 640, Height = 480 };
        _mapperMock.Setup(m => m.Map<CameraInfoDTO>(_cameraWorkerMock.Object)).Returns(dto);

        var result = manager.GetWorkerDtos().ToList();

        Assert.Single(result);
        Assert.Equal(dto, result[0]);
    }

    [Fact]
    public void GetWorkerIds_ShouldReturnIds()
    {
        var manager = CreateManager();
        var cts = new CancellationTokenSource();
        var handle = manager.Create(_workerProps, cts.Token);

        // Mark the worker as active
        handle.RunningTask = Task.CompletedTask;

        var ids = manager.GetWorkerIds().ToList();

        Assert.Single(ids);
        Assert.Equal(1, ids[0]);
    }

    [Fact]
    public void GetWorkerIds_ShouldReturnIds_WhenInactive()
    {
        var manager = CreateManager();
        var cts = new CancellationTokenSource();
        var handle = manager.Create(_workerProps, cts.Token);

        // Mark the worker as inactive
        handle.RunningTask = null;

        var ids = manager.GetWorkerIds(false).ToList();

        Assert.Single(ids);
        Assert.Equal(1, ids[0]);
    }
}