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

        _workerFactoryMock.Setup(f => f.Create(It.IsAny<WorkerProperties>()))
            .Returns(_cameraWorkerMock.Object);
    }

    private CameraWorkerManager CreateManager() => new(_workerFactoryMock.Object, _mapperMock.Object, _loggerMock.Object);

    [Fact]
    public void Create_ShouldAddAndWorkerHandle()
    {
        var manager = CreateManager();
        var cts = new CancellationTokenSource();

        // Act: create the handle (no return value)
        manager.CreateWorker(_workerProps, cts.Token);
        var workerHandles = GetWorkerHandles(manager);

        // Assert: handle exists and contains the mock worker
        Assert.True(workerHandles.TryGetValue(_workerProps.CameraOptions.Id, out var handle));
        Assert.NotNull(handle);
        Assert.Equal(_cameraWorkerMock.Object, handle.Worker);

        // Act: call Create again with the same camera ID
        manager.CreateWorker(_workerProps, cts.Token);

        // Assert: the handle instance is the same
        Assert.True(workerHandles.TryGetValue(_workerProps.CameraOptions.Id, out var handle2));
        Assert.Same(handle, handle2);
    }

    [Fact]
    public async Task StartAsync_ShouldCallStartAndLog()
    {
        var manager = CreateManager();
        var workerHandles = GetWorkerHandles(manager);
        workerHandles[1] = _workerHandleMock.Object;

        // Setup the mock
        _workerHandleMock.Setup(h => h.StartAsync()).Returns(Task.CompletedTask).Verifiable();

        await manager.StartAsync(_workerHandleMock.Object.Worker.CamId);

        _workerHandleMock.Verify(h => h.StartAsync(), Times.Once);
    }

    [Fact]
    public async Task StopAsync_ShouldRemoveAndStopWorker()
    {
        var manager = CreateManager();
        var workerHandles = GetWorkerHandles(manager);
        workerHandles[1] = _workerHandleMock.Object;

        // Setup the mock
        _workerHandleMock.Setup(h => h.StopAsync()).Returns(Task.CompletedTask).Verifiable();

        await manager.StopAsync(1);

        _workerHandleMock.Verify(h => h.StopAsync(), Times.Once);
    }

    [Fact]
    public async Task StopAllAsync_ShouldStopAllWorkers()
    {
        var manager = CreateManager();
        var workerHandles = GetWorkerHandles(manager);
        workerHandles[1] = _workerHandleMock.Object;
        //TODO: add more

        // Setup the mock
        _workerHandleMock.Setup(h => h.StopAsync()).Returns(Task.CompletedTask).Verifiable();

        await manager.StopAllAsync();

        _workerHandleMock.Verify(h => h.StopAsync(), Times.Once);
    }

    [Fact]
    public void GetWorkerDtos_ShouldReturnMappedDtos()
    {
        var manager = CreateManager();
        var cts = new CancellationTokenSource();

        // Add the worker handle
        manager.CreateWorker(_workerProps, cts.Token);

        var workerHandles = GetWorkerHandles(manager);

        // Mark the worker as active
        Assert.True(workerHandles.TryGetValue(_workerProps.CameraOptions.Id, out var handle));
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

        // Add the worker handle
        manager.CreateWorker(_workerProps, cts.Token);

        var workerHandles = GetWorkerHandles(manager);

        // Mark the worker as active
        Assert.True(workerHandles.TryGetValue(_workerProps.CameraOptions.Id, out var handle));
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

        // Add the worker handle
        manager.CreateWorker(_workerProps, cts.Token);

        var workerHandles = GetWorkerHandles(manager);

        // Get and mark the worker as inactive
        Assert.True(workerHandles.TryGetValue(_workerProps.CameraOptions.Id, out var handle));
        handle.RunningTask = null;

        var ids = manager.GetWorkerIds(isActive: false).ToList();

        Assert.Single(ids);
        Assert.Equal(1, ids[0]);
    }

    [Fact]
    public void GetWorkerIds_ShouldReturnIds_WhenActive()
    {
        var manager = CreateManager();
        var cts = new CancellationTokenSource();

        // Add the worker handle
        manager.CreateWorker(_workerProps, cts.Token);

        var workerHandles = GetWorkerHandles(manager);

        // Get and mark the worker as active
        Assert.True(workerHandles.TryGetValue(_workerProps.CameraOptions.Id, out var handle));
        handle.RunningTask = Task.CompletedTask;

        var ids = manager.GetWorkerIds(isActive: true).ToList();

        Assert.Single(ids);
        Assert.Equal(1, ids[0]);
    }

    private static ConcurrentDictionary<int, IWorkerHandle> GetWorkerHandles(CameraWorkerManager manager)
    {
        // Access the private _workerHandles dictionary
        var workerHandlesField = typeof(CameraWorkerManager)
            .GetField("_workerHandles", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (ConcurrentDictionary<int, IWorkerHandle>)workerHandlesField!.GetValue(manager)!;
    }
}