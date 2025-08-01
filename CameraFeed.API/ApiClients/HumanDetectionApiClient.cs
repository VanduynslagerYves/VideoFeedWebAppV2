using System.Net.Http.Headers;

namespace CameraFeed.API.ApiClients;

public interface IHumanDetectionApiClient
{
    Task<byte[]> DetectHumansAsync(byte[] imageData);
}

public abstract class HumanDetectionApiClientBase(IHttpClientFactory httpClientFactory) : IHumanDetectionApiClient
{
    protected readonly IHttpClientFactory HttpClientFactory = httpClientFactory;

    public abstract Task<byte[]> DetectHumansAsync(byte[] imageData);
}

public class HumanDetectionApiClient(IHttpClientFactory httpClientFactory, ILogger<HumanDetectionApiClientBase> logger) : HumanDetectionApiClientBase(httpClientFactory)
{
    private readonly ILogger<HumanDetectionApiClientBase> _logger = logger;

    public override async Task<byte[]> DetectHumansAsync(byte[] imageData)
    {
        //TODO: Add error handling and logging, return original bytes if something goes wrong
        using var client = HttpClientFactory.CreateClient("HumanDetectionApi");
        using var content = new ByteArrayContent(imageData);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        var response = await client.PostAsync("http://127.0.0.1:8000/detect-human-v2/", content);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync();
    }
}
