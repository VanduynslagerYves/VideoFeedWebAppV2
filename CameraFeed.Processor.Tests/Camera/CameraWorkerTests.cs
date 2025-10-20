using Moq;
using Microsoft.Extensions.Logging;
using CameraFeed.Processor.Camera;
using CameraFeed.Processor.Camera.Factories;
using CameraFeed.Processor.Clients.SignalR;

namespace CameraFeed.Processor.Tests.Camera;

public class CameraWorkerTests
{
    private static WorkerProperties GetWorkerProperties() => new()
    {
        Mode = InferenceMode.Continuous,
        CameraOptions = new CameraProperties
        {
            Id = 1,
            Name = "TestCam",
            Resolution = new CameraResolutionProperties { Width = 640, Height = 480 },
            Framerate = 30
        },
        MotionDetectionOptions = new MotionDetectionProperties
        {
            DownscaleFactor = 2,
            MotionRatio = 0.1
        }
    };

    [Fact]
    public async Task RunAsync_SendsFrameToLocalAndRemote_WhenRemoteStreamingEnabled()
    {
        var tokenSource = new CancellationTokenSource();
        var mockSignalR = new Mock<ICameraSignalRclient>();
        var mockFrameProcessor = new Mock<IFrameProcessor>();
        var mockFactory = new Mock<IFrameProcessorFactory>();
        var mockLogger = new Mock<ILogger<CameraWorker>>();

        mockSignalR.Setup(x => x.CreateConnectionsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockSignalR.Setup(x => x.IsRemoteStreamingEnabled(It.IsAny<string>())).Returns(true);
        mockSignalR.Setup(x => x.SendFrameToLocalAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockSignalR.Setup(x => x.SendFrameToRemoteAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockSignalR.Setup(x => x.StopAndDisposeConnectionsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mockFrameProcessor.SetupSequence(x => x.QueryAndProcessFrame(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([1, 2, 3])
            .ThrowsAsync(new OperationCanceledException());

        mockFactory.Setup(x => x.CreateAsync(It.IsAny<WorkerProperties>(), It.IsAny<BackgroundSubtractorType>()))
            .ReturnsAsync(mockFrameProcessor.Object);

        var worker = new CameraWorker(GetWorkerProperties(), mockSignalR.Object, mockFactory.Object, mockLogger.Object);

        await worker.RunAsync(tokenSource.Token);

        mockSignalR.Verify(x => x.SendFrameToLocalAsync(It.IsAny<byte[]>(), "TestCam", It.IsAny<CancellationToken>()), Times.AtLeastOnce());
        mockSignalR.Verify(x => x.SendFrameToRemoteAsync(It.IsAny<byte[]>(), "TestCam", It.IsAny<CancellationToken>()), Times.AtLeastOnce());
    }

    [Fact]
    public async Task RunAsync_LogsCancellation()
    {
        var tokenSource = new CancellationTokenSource();
        var mockSignalR = new Mock<ICameraSignalRclient>();
        var mockFrameProcessor = new Mock<IFrameProcessor>();
        var mockFactory = new Mock<IFrameProcessorFactory>();
        var mockLogger = new Mock<ILogger<CameraWorker>>();

        mockSignalR.Setup(x => x.CreateConnectionsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockSignalR.Setup(x => x.IsRemoteStreamingEnabled(It.IsAny<string>())).Returns(false);
        mockSignalR.Setup(x => x.SendFrameToLocalAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockSignalR.Setup(x => x.StopAndDisposeConnectionsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mockFrameProcessor.Setup(x => x.QueryAndProcessFrame(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        mockFactory.Setup(x => x.CreateAsync(It.IsAny<WorkerProperties>(), It.IsAny<BackgroundSubtractorType>()))
            .ReturnsAsync(mockFrameProcessor.Object);

        var worker = new CameraWorker(GetWorkerProperties(), mockSignalR.Object, mockFactory.Object, mockLogger.Object);

        await worker.RunAsync(tokenSource.Token);

        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("was cancelled")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce());
    }

    [Fact]
    public async Task RunAsync_LogsError_OnException()
    {
        var tokenSource = new CancellationTokenSource();
        var mockSignalR = new Mock<ICameraSignalRclient>();
        var mockFrameProcessor = new Mock<IFrameProcessor>();
        var mockFactory = new Mock<IFrameProcessorFactory>();
        var mockLogger = new Mock<ILogger<CameraWorker>>();

        mockSignalR.Setup(x => x.CreateConnectionsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test error"));

        mockFactory.Setup(x => x.CreateAsync(It.IsAny<WorkerProperties>(), It.IsAny<BackgroundSubtractorType>()))
            .ReturnsAsync(mockFrameProcessor.Object);

        var worker = new CameraWorker(GetWorkerProperties(), mockSignalR.Object, mockFactory.Object, mockLogger.Object);

        await worker.RunAsync(tokenSource.Token);

        mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("An error occurred")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce());
    }
}