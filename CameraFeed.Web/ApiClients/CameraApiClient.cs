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
    /// <summary>
    /// Starts the camera with the specified camera ID asynchronously.
    /// </summary>
    /// <param name="cameraId">The unique identifier of the camera to start. Must correspond to a valid camera ID.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a <see
    /// cref="CameraApiOperationResult"/> object indicating the outcome of the operation, or <see langword="null"/> if
    /// the operation could not be completed.</returns>
    Task<CameraApiOperationResult?> StartCameraAsync(int cameraId);
    /// <summary>
    /// Sets the access token used for authenticating API requests.
    /// </summary>
    /// <remarks>The access token must be a valid token issued by the authentication provider.  Ensure that
    /// the token is refreshed or updated as needed to maintain access to the API.</remarks>
    /// <param name="accessToken">The access token to be used for authentication. This value cannot be null or empty.</param>
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
public class CameraApiClient(IHttpClientFactory httpClientFactory, ILogger<CameraApiClient> _logger) : CameraApiClientBase(httpClientFactory)
{
    protected readonly ILogger<CameraApiClient> _logger = _logger;

    /// <summary>
    /// Sends a request to start the specified camera using the Camera API.
    /// </summary>
    /// <remarks>This method sends an HTTP POST request to the Camera API to start the camera identified by
    /// <paramref name="cameraId"/>. The request includes a Bearer token for authentication. If the operation is
    /// successful, the method returns a deserialized <see cref="CameraApiOperationResult"/> object. If the request
    /// fails, times out, or encounters an unexpected error, the method logs the error and returns <see
    /// langword="null"/>.</remarks>
    /// <param name="cameraId">The unique identifier of the camera to start.</param>
    /// <returns>A <see cref="CameraApiOperationResult"/> object containing the result of the operation,  or <see
    /// langword="null"/> if the operation fails or the response cannot be deserialized.</returns>
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

            response.EnsureSuccessStatusCode();
            return responseObject;
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
