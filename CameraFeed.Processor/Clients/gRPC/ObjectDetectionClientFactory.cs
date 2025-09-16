using Grpc.Net.Client;

namespace CameraFeed.Processor.Clients.gRPC;

public static class ObjectDetectionClientFactory
{
    public static (GrpcChannel channel, ObjectDetection.ObjectDetectionClient client) CreateWithChannel(ObjectDetectionClientOptions? options = null)
    {
        options ??= new ObjectDetectionClientOptions();

        var channel = GrpcChannel.ForAddress(options.Address);
        var client = new ObjectDetection.ObjectDetectionClient(channel);
        return (channel, client);
    }
}

public class ObjectDetectionClientOptions
{
    public string Address { get; set; } = "http://127.0.0.1:50051";
}