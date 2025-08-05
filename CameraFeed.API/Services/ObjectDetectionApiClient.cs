using System.Net.Http.Headers;

namespace CameraFeed.API.Services;

public interface IObjectDetectionApiClient
{
    Task<byte[]> DetectObjectsAsync(byte[] imageData);
}

public abstract class ObjectDetectionApiClientBase(IHttpClientFactory httpClientFactory) : IObjectDetectionApiClient
{
    protected readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    protected readonly HttpClient _httpClient = httpClientFactory.CreateClient("HumanDetectionApi");

    public abstract Task<byte[]> DetectObjectsAsync(byte[] imageData);
}

public class ObjectDetectionApiClient(IHttpClientFactory httpClientFactory, ILogger<ObjectDetectionApiClient> logger) : ObjectDetectionApiClientBase(httpClientFactory)
{
    private readonly ILogger<ObjectDetectionApiClient> _logger = logger;
    private bool _apiAvailable = true;
    private int _requestCounter = 0;

    public override async Task<byte[]> DetectObjectsAsync(byte[] imageData)
    {
        _requestCounter++; //TODO: use a healthcheck endpoint with a timer
        if(!_apiAvailable && _requestCounter <= 300) //after 300 frames
        {
            //_logger.LogWarning("Object detection API is not available. Returning original image data.");
            return imageData;
        }

        _requestCounter = 0;
        using var content = new ByteArrayContent(imageData);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        try
        {
            var response = await _httpClient.PostAsync("http://127.0.0.1:8000/detect-objects/", content);
            response.EnsureSuccessStatusCode();

            _apiAvailable = true;
            return await response.Content.ReadAsByteArrayAsync();
        }
        catch(HttpRequestException)
        {
            _apiAvailable = false;
            _logger.LogWarning("HTTP request for detecting objects failed.");
            return imageData;
        }
        catch (Exception)
        {
            _apiAvailable = false;
            _logger.LogWarning("Unexpected error when detecting humans.");
            return imageData;
        }
    }
}
