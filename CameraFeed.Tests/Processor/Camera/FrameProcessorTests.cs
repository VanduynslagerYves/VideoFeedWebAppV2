using CameraFeed.Processor.Camera;
using CameraFeed.Processor.Camera.Adapter;
using CameraFeed.Processor.Clients.gRPC;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Microsoft.Extensions.Logging;
using Moq;

namespace CameraFeed.Tests.Processor.Camera;

public class FrameProcessorTests
{
    private readonly Mat _matMock;

    public FrameProcessorTests()
    {
        _matMock = CreateRandomMat();
    }

    [Fact]
    public async Task QueryAndProcessFrame_ReturnsNull_WhenCaptureIsNullOrNotOpened()
    {
        var captureMock = new Mock<IVideoCaptureAdapter>();
        captureMock.Setup(c => c.IsOpened).Returns(false);

        var processor = CreateProcessor(captureMock.Object);

        var result = await processor.QueryAndProcessFrame();
        Assert.Null(result);
    }

    [Fact]
    public async Task QueryAndProcessFrame_ReturnsNull_WhenFrameIsEmpty()
    {
        var captureMock = new Mock<IVideoCaptureAdapter>();
        captureMock.Setup(c => c.IsOpened).Returns(true);
        captureMock.Setup(c => c.QueryFrame()).Returns((Mat)null!);

        var processor = CreateProcessor(captureMock.Object);

        var result = await processor.QueryAndProcessFrame();
        Assert.Null(result);
    }

    [Fact]
    public async Task QueryAndProcessFrame_RunsInference_WhenModeIsContinuous()
    {
        var captureMock = new Mock<IVideoCaptureAdapter>();
        captureMock.Setup(c => c.IsOpened).Returns(true);

        //var matMock = CreateRandomMat();
        captureMock.Setup(c => c.QueryFrame()).Returns(_matMock);

        var subtractorMock = new Mock<IBackgroundSubtractorAdapter>();
        var grpcClientMock = new Mock<IObjectDetectionGrpcClient>();
        grpcClientMock.Setup(g => g.DetectObjectsAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[] data, CancellationToken _) => data);

        var loggerMock = new Mock<ILogger<FrameProcessor>>();
        var options = GetWorkerProperties(InferenceMode.Continuous);

        var processor = new FrameProcessor(captureMock.Object, subtractorMock.Object, grpcClientMock.Object, options, loggerMock.Object);

        // Emgu.CV Image<TColor, TDepth> ToJpegData is extension, so we need to mock it or skip actual conversion
        // For this test, assume ToJpegData returns a non-empty byte array
        // We may need to refactor FrameProcessor for testability if ToJpegData is not mockable

        var result = await processor.QueryAndProcessFrame();
        // If inference runs, result should not be null
        Assert.NotNull(result);
        grpcClientMock.Verify(g => g.DetectObjectsAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task QueryAndProcessFrame_DropsFrame_WhenSizeExceedsMaxSize()
    {
        var captureMock = new Mock<IVideoCaptureAdapter>();
        captureMock.Setup(c => c.IsOpened).Returns(true);

        //var matMock = new Mat();
        captureMock.Setup(c => c.QueryFrame()).Returns(_matMock);

        var subtractorMock = new Mock<IBackgroundSubtractorAdapter>();
        var grpcClientMock = new Mock<IObjectDetectionGrpcClient>();
        grpcClientMock.Setup(g => g.DetectObjectsAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new byte[300 * 1024]); // Exceeds default maxSize

        var loggerMock = new Mock<ILogger<FrameProcessor>>();
        var options = GetWorkerProperties(InferenceMode.Continuous);

        var processor = new FrameProcessor(captureMock.Object, subtractorMock.Object, grpcClientMock.Object, options, loggerMock.Object);

        var result = await processor.QueryAndProcessFrame(maxSize: 200 * 1024);
        Assert.Null(result);
        loggerMock.Verify(l => l.Log(
            Microsoft.Extensions.Logging.LogLevel.Warning,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()), Times.AtLeastOnce);
    }

    [Fact]
    public void Dispose_CallsDisposeOnDependencies()
    {
        var captureMock = new Mock<IVideoCaptureAdapter>();
        var subtractorMock = new Mock<IBackgroundSubtractorAdapter>();
        var grpcClientMock = new Mock<IObjectDetectionGrpcClient>();
        var loggerMock = new Mock<ILogger<FrameProcessor>>();
        var options = GetWorkerProperties(InferenceMode.Continuous);

        var processor = new FrameProcessor(captureMock.Object, subtractorMock.Object, grpcClientMock.Object, options, loggerMock.Object);

        processor.Dispose();

        captureMock.Verify(c => c.Dispose(), Times.Once);
        subtractorMock.Verify(s => s.Dispose(), Times.Once);
        grpcClientMock.Verify(g => g.Dispose(), Times.Once);
    }

    #region Helpers
    private static FrameProcessor CreateProcessor(IVideoCaptureAdapter capture)
    {
        var subtractorMock = new Mock<IBackgroundSubtractorAdapter>();
        var grpcClientMock = new Mock<IObjectDetectionGrpcClient>();
        var loggerMock = new Mock<ILogger<FrameProcessor>>();
        var options = GetWorkerProperties();

        return new FrameProcessor(capture, subtractorMock.Object, grpcClientMock.Object, options, loggerMock.Object);
    }

    private static WorkerProperties GetWorkerProperties(InferenceMode mode = InferenceMode.Continuous) => new()
    {
        Mode = mode,
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
            MotionRatio = 0.05
        }
    };

    private static Mat CreateRandomMat(int width = 640, int height = 480, int channels = 3)
    {
        var mat = new Mat(height, width, DepthType.Cv8U, channels);
        var rng = new Random();

        // Create a byte array for all pixels
        byte[] data = new byte[width * height * channels];
        rng.NextBytes(data);

        // Copy data to Mat
        mat.SetTo<byte>(data);

        return mat;
    }
    #endregion
}
