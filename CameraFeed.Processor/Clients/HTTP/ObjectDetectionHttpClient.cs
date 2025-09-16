using System.Net.Http.Headers;

namespace CameraFeed.Processor.Clients.HTTP;

[Obsolete("Use GRPC client instead")]
public interface IObjectDetectionHttpClient
{
    Task<byte[]> DetectObjectsAsync(byte[] imageData, string cameraId);
}

[Obsolete("Use GRPC client instead")]
public abstract class ObjectDetectionHttpClientBase(IHttpClientFactory httpClientFactory) : IObjectDetectionHttpClient
{
    protected readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    protected readonly HttpClient _httpClient = httpClientFactory.CreateClient("HumanDetectionApi");

    public abstract Task<byte[]> DetectObjectsAsync(byte[] imageData, string cameraId);
}

[Obsolete("Use GRPC client instead")]
public class ObjectDetectionHttpClient(IHttpClientFactory httpClientFactory, ILogger<ObjectDetectionHttpClient> logger) : ObjectDetectionHttpClientBase(httpClientFactory)
{
    private readonly ILogger<ObjectDetectionHttpClient> _logger = logger;
    private bool _apiAvailable = true;
    private int _requestCounter = 0;

    public override async Task<byte[]> DetectObjectsAsync(byte[] imageData, string cameraId)
    {
        _requestCounter++; //TODO: use a healthcheck endpoint with a timer
        if (!_apiAvailable && _requestCounter <= 300) return imageData;//return original image data after 300 frames
        _requestCounter = 0;

        var request = new HttpRequestMessage(HttpMethod.Post, "http://127.0.0.1:8000/detect-objects/")
        {
            Content = new ByteArrayContent(imageData)
        };
        request.Headers.Add("X-Camera-Id", cameraId);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        try
        {
            var response = await _httpClient.SendAsync(request);
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
