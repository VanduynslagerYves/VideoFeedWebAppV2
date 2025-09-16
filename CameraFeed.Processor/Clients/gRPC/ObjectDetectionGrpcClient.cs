using Google.Protobuf;
using Grpc.Net.Client;
using Grpc.Core;
using Polly.CircuitBreaker;

namespace CameraFeed.Processor.Clients.gRPC;

public interface IObjectDetectionGrpcClient
{
    Task<byte[]> DetectObjectsAsync(byte[] imageData, CancellationToken cancellation = default);
}

public class ObjectDetectionGrpcClient : IObjectDetectionGrpcClient, IDisposable
{
    private readonly ILogger<ObjectDetectionGrpcClient> _logger;
    private readonly GrpcChannel _channel;
    private readonly ObjectDetection.ObjectDetectionClient _client;
    private readonly AsyncCircuitBreakerPolicy _circuitBreakerPolicy;

    public ObjectDetectionGrpcClient(ILogger<ObjectDetectionGrpcClient> logger)
    {
        _logger = logger;
        (_channel, _client) = ObjectDetectionClientFactory.CreateWithChannel();
        _circuitBreakerPolicy = CircuitBreakerPolicyFactory.CreateGrpcCircuitBreakerPolicy(_logger);
    }

    public async Task<byte[]> DetectObjectsAsync(byte[] imageData, CancellationToken cancellationToken = default)
    {
        if (_circuitBreakerPolicy.CircuitState == CircuitState.Open) return imageData;

        try
        {
            return await _circuitBreakerPolicy.ExecuteAsync(async ct =>
            {
                using var call = _client.DetectObjects(cancellationToken: cancellationToken);
                await call.RequestStream.WriteAsync(new ImageRequest { ImageData = ByteString.CopyFrom(imageData) }, cancellationToken);
                await call.RequestStream.CompleteAsync();

                if (await call.ResponseStream.MoveNext(cancellationToken))
                {
                    var response = call.ResponseStream.Current;
                    return response.ProcessedImage.ToByteArray();
                }

                _logger.LogWarning("No response received from gRPC server.");
                return imageData;
            }, cancellationToken);
        }
        catch (RpcException ex)
        {
            //_logger.LogWarning(ex, "gRPC request failed when detecting objects.");
            return imageData;
        }
        catch (Exception ex)
        {
            //_logger.LogError(ex, "Unexpected error when detecting objects via gRPC.");
            return imageData;
        }
    }

    //In ASP.NET Core, when you register a service as a singleton and it implements IDisposable, the DI container will automatically call Dispose() on application shutdown.
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _channel?.Dispose();
    }
}
