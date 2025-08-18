using Grpc.Net.Client;

namespace CameraFeed.Processor.Services;

public interface IObjectDetectionClient
{
    Task<byte[]> DetectObjectsAsync(byte[] imageData, CancellationToken cancellation = default);
}

public class ObjectDetectionClient: IObjectDetectionClient, IDisposable
{
    private readonly ILogger<ObjectDetectionClient> _logger;
    private readonly GrpcChannel _channel;
    private readonly ObjectDetection.ObjectDetectionClient _client;

    private readonly bool _apiAvailable = true;
    private int _requestCounter = 0;

    public ObjectDetectionClient(ILogger<ObjectDetectionClient> logger)
    {
        _logger = logger;
        _channel = GrpcChannel.ForAddress("http://127.0.0.1:50051");
        _client = new ObjectDetection.ObjectDetectionClient(_channel);
    }

    public async Task<byte[]> DetectObjectsAsync(byte[] imageData, CancellationToken cancellationToken = default)
    {
        _requestCounter++; //TODO: use a healthcheck endpoint with a timer
        if (!_apiAvailable && _requestCounter <= 300) //after 300 frames
        {
            //_logger.LogWarning("Object detection API is not available. Returning original image data.");
            return imageData;
        }

        _requestCounter = 0;

        try
        {
            using var call = _client.DetectObjects(cancellationToken: cancellationToken);

            await call.RequestStream.WriteAsync(new ImageRequest { ImageData = Google.Protobuf.ByteString.CopyFrom(imageData) }, cancellationToken);
            await call.RequestStream.CompleteAsync();

            if (await call.ResponseStream.MoveNext(cancellationToken))
            {
                var response = call.ResponseStream.Current;
                return response.ProcessedImage.ToByteArray();
            }
            else
            {
                _logger.LogWarning("No response received from gRPC server.");
                return imageData;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "gRPC request failed when detecting humans.");
            return imageData;
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _channel?.Dispose();
    }
}
