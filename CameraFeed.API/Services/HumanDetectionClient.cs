using Grpc.Net.Client;

namespace CameraFeed.API.Services;

public interface IHumanDetectionClient
{
    Task<byte[]> DetectHumansAsync(byte[] imageData, CancellationToken cancellation = default);
}

public class HumanDetectionClient: IHumanDetectionClient, IDisposable
{
    private readonly ILogger<HumanDetectionClient> _logger;
    private readonly GrpcChannel _channel;
    private readonly HumanDetection.HumanDetectionClient _client;

    public HumanDetectionClient(ILogger<HumanDetectionClient> logger)
    {
        _logger = logger;
        _channel = GrpcChannel.ForAddress("http://127.0.0.1:50051");
        _client = new HumanDetection.HumanDetectionClient(_channel);
    }

    public async Task<byte[]> DetectHumansAsync(byte[] imageData, CancellationToken cancellationToken = default)
    {
        try
        {
            using var call = _client.DetectHumans(cancellationToken: cancellationToken);

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
        _channel?.Dispose();
    }
}
