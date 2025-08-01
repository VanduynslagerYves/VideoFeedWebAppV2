using CameraFeed.Web.ApiClients.ViewModels;
using Newtonsoft.Json;
using System.Net.Http.Headers;

namespace CameraFeed.Web.ApiClients;

/// <summary>
/// Defines methods for interacting with a camera API, including starting a camera and setting an access token for
/// authentication.
/// </summary>
public interface ICameraApiClient
{
    Task<CameraApiOperationResult?> StartCameraAsync(int cameraId);
    void SetAccessToken(string accessToken);
}

/// <summary>
/// Provides a base class for API clients that interact with camera services.
/// </summary>
/// <remarks>This abstract class defines the core functionality for API clients, including methods for starting
/// cameras and setting authentication tokens. Implementations should provide specific details for interacting with
/// different camera APIs. The class utilizes an <see cref="IHttpClientFactory"/> to manage HTTP client instances,
/// ensuring efficient resource usage and connection management.</remarks>
/// <param name="httpClientFactory"></param>
public abstract class CameraApiClientBase(IHttpClientFactory httpClientFactory) : ICameraApiClient
{
    protected readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    protected string? _accessToken;

    /// <summary>
    /// Initiates the start of a camera with the specified identifier asynchronously.
    /// </summary>
    /// <remarks>This method sends a POST request to the camera API to start the camera. Ensure that the <see
    /// cref="_accessToken"/> is valid and the camera API is accessible.</remarks>
    /// <param name="cameraId">The unique identifier of the camera to start.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the HTTP response message from the
    /// camera API.</returns>
    public abstract Task<CameraApiOperationResult?> StartCameraAsync(int cameraId);

    /// <summary>
    /// Sets the access token for the API client.
    /// </summary>
    /// <param name="accessToken">The access token to be used for authentication.</param>
    public void SetAccessToken(string accessToken)
    {
        _accessToken = accessToken;
    }
}

/// <summary>
/// Provides methods to interact with a camera API, allowing operations such as starting a camera.
/// </summary>
/// <remarks>This client uses an <see cref="IHttpClientFactory"/> to create HTTP clients for making requests to
/// the camera API. An access token must be set using <see cref="SetAccessToken"/> before making requests that require
/// authentication.</remarks>
/// <param name="httpClientFactory"></param>
public class CameraApiClient(IHttpClientFactory httpClientFactory, ILogger<CameraApiClientBase> _logger) : CameraApiClientBase(httpClientFactory)
{
    protected readonly ILogger<CameraApiClientBase> _logger = _logger;

    public override async Task<CameraApiOperationResult?> StartCameraAsync(int cameraId)
    {
        using var httpClient = _httpClientFactory.CreateClient("CameraApi");
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

        //TODO: use appsettings to get the base URL instead of hardcoding it
        var request = new HttpRequestMessage(HttpMethod.Post, $"https://localhost:7214/api/camera/startcam/{cameraId}");
        //var request = new HttpRequestMessage(HttpMethod.Post, $"https://pure-current-mastodon.ngrok-free.app/api/camera/startcam/{cameraId}");

        try
        {
            var response = await httpClient.SendAsync(request);

            var responseString = await response.Content.ReadAsStringAsync();
            var responseObject = JsonConvert.DeserializeObject<CameraApiOperationResult>(responseString);

            if (responseObject is null)
            {
                _logger.LogError("Failed to deserialize response from camera API.");
                return null;
            }

            if (response.IsSuccessStatusCode) return responseObject;
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed when starting camera {CameraId}.", cameraId);
            return null;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "HTTP request timed out when starting camera {CameraId}.", cameraId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error when starting camera {CameraId}.", cameraId);
            return null;
        }
    }
}
