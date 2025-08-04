using System.Net.Http.Headers;

namespace CameraFeed.API.Services;

/// <summary>
/// Defines an API client for detecting humans in an image.
/// </summary>
/// <remarks>This interface provides a method to analyze image data and determine the presence of humans. The
/// implementation of this interface is responsible for handling the detection logic.</remarks>
public interface IObjectDetectionApiClient
{
    /// <summary>
    /// Detects human figures in the provided image data.
    /// </summary>
    /// <remarks>This method performs human detection using image analysis techniques. The input image should
    /// be in a valid format (e.g., JPEG, PNG) and of sufficient quality for detection. The caller is  responsible for
    /// ensuring the input data meets these requirements.</remarks>
    /// <param name="imageData">The image data to analyze, represented as a byte array. The image must be in a supported format.</param>
    /// <returns>A byte array containing the processed image with detected human figures highlighted.</returns>
    Task<byte[]> DetectObjectsAsync(byte[] imageData);
}

/// <summary>
/// Provides a base class for clients that interact with a human detection API.
/// </summary>
/// <remarks>This abstract class defines the core functionality for detecting humans in images and requires
/// derived classes to implement the <see cref="DetectObjectsAsync"/> method.</remarks>
/// <param name="httpClientFactory"></param>
public abstract class ObjectDetectionApiClientBase(IHttpClientFactory httpClientFactory) : IObjectDetectionApiClient
{
    protected readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    //protected readonly HttpClient _httpClient = httpClientFactory.CreateClient("HumanDetectionApi");

    /// <summary>
    /// Analyzes the provided image data to detect human figures and returns the processed image.
    /// </summary>
    /// <remarks>The method performs human detection on the provided image data and highlights detected
    /// figures in the output image. The input image data should be in a supported format, such as JPEG or
    /// PNG.</remarks>
    /// <param name="imageData">The image data to analyze, represented as a byte array. Must not be null or empty.</param>
    /// <returns>A byte array containing the processed image with detected human figures highlighted.</returns>
    public abstract Task<byte[]> DetectObjectsAsync(byte[] imageData);
}

/// <summary>
/// Provides functionality to detect humans in an image by interacting with an external human detection API.
/// </summary>
/// <remarks>This client sends image data to a configured external API endpoint for human detection processing. If
/// the API call fails or times out, the original image data is returned as a fallback.</remarks>
/// <param name="httpClientFactory"></param>
/// <param name="logger"></param>
public class ObjectDetectionApiClient(IHttpClientFactory httpClientFactory, ILogger<ObjectDetectionApiClient> logger) : ObjectDetectionApiClientBase(httpClientFactory)
{
    private readonly ILogger<ObjectDetectionApiClient> _logger = logger;

    /// <summary>
    /// Sends an image to a human detection API and retrieves the processed image with detected humans highlighted.
    /// </summary>
    /// <remarks>This method uses an HTTP client to send the image data to a human detection API endpoint.  If
    /// the API request fails due to an HTTP error, timeout, or unexpected exception, the original image data is
    /// returned.</remarks>
    /// <param name="imageData">The image data to be analyzed, represented as a byte array.</param>
    /// <returns>A byte array containing the processed image with detected humans highlighted.  If the request fails or an error
    /// occurs, the original <paramref name="imageData"/> is returned.</returns>
    public override async Task<byte[]> DetectObjectsAsync(byte[] imageData)
    {
        using var httpClient = _httpClientFactory.CreateClient("HumanDetectionApi");
        using var content = new ByteArrayContent(imageData);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        try
        {
            var response = await httpClient.PostAsync("http://127.0.0.1:8000/detect-objects/", content);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsByteArrayAsync();
        }
        catch (HttpRequestException)
        {
            _logger.LogWarning("HTTP request failed when detecting humans.");
            return imageData;
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("HTTP request timed out when detecting humans.");
            return imageData;
        }
        catch (Exception)
        {
            _logger.LogWarning("Unexpected error when detecting humans.");
            return imageData;
        }
    }
}
