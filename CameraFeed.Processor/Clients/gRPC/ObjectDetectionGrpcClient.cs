using Google.Protobuf;
using Grpc.Net.Client;
using Grpc.Core;
using Polly.CircuitBreaker;

namespace CameraFeed.Processor.Clients.gRPC;

public interface IObjectDetectionGrpcClient: IDisposable
{
    Task<byte[]> DetectObjectsAsync(byte[] imageData, CancellationToken cancellation = default);
}

public class ObjectDetectionGrpcClient : IObjectDetectionGrpcClient
{
    private readonly ILogger<ObjectDetectionGrpcClient> _logger;
    private readonly GrpcChannel _channel;
    private readonly ObjectDetection.ObjectDetectionClient _client;
    private readonly AsyncCircuitBreakerPolicy _circuitBreakerPolicy;

    public ObjectDetectionGrpcClient(ILogger<ObjectDetectionGrpcClient> logger)
    {
        _logger = logger;
        (_channel, _client) = ObjectDetectionClientFactory.CreateWithChannel();
        _circuitBreakerPolicy = CircuitBreakerFactory.CreateCircuitBreakerPolicy<RpcException>("gRPC", _logger);
    }

    public async Task<byte[]> DetectObjectsAsync(byte[] imageData, CancellationToken token = default)
    {
        if (_circuitBreakerPolicy.CircuitState == CircuitState.Open) return imageData;

        try
        {
            return await _circuitBreakerPolicy.ExecuteAsync(async ct =>
            {
                using var call = _client.DetectObjects(cancellationToken: token);
                await call.RequestStream.WriteAsync(new ImageRequest { ImageData = ByteString.CopyFrom(imageData) }, token);
                await call.RequestStream.CompleteAsync();

                if (await call.ResponseStream.MoveNext(token))
                {
                    var response = call.ResponseStream.Current;
                    return response.ProcessedImage.ToByteArray();
                }

                _logger.LogWarning("No response received from gRPC server.");
                return imageData;
            }, token);
        }
        //catch(RpcException)
        //{
        //    return imageData;
        //}
        catch (Exception)
        {
            return imageData;
        }
    }

    //In ASP.NET Core, when you register a service as a singleton and it implements IDisposable, the DI container will automatically call Dispose() on application shutdown.
    public void Dispose()
    {
        GC.SuppressFinalize(this);

        // Disposing the gRPC channel is important to release underlying resources such as sockets and HTTP connections.
        // This helps prevent resource leaks, especially in long-running applications.
        _channel?.Dispose();
    }
}
