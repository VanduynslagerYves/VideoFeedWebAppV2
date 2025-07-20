using System.Net.Http.Headers;

namespace CameraFeed.Web.ApiClients;

/// <summary>
/// Defines methods for interacting with a camera API, including starting a camera and setting an access token for
/// authentication.
/// </summary>
public interface IApiClient
{
    Task<bool> StartCameraAsync(int cameraId);
    void SetAccessToken(string accessToken);
}

/// <summary>
/// Provides methods to interact with a camera API, allowing operations such as starting a camera.
/// </summary>
/// <remarks>This client uses an <see cref="IHttpClientFactory"/> to create HTTP clients for making requests to
/// the camera API. An access token must be set using <see cref="SetAccessToken"/> before making requests that require
/// authentication.</remarks>
/// <param name="httpClientFactory"></param>
public class CameraApiClient(IHttpClientFactory httpClientFactory) : IApiClient
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private string? _accessToken;

    /// <summary>
    /// Sets the access token used for authentication.
    /// </summary>
    /// <param name="accessToken">The access token to be used for subsequent requests. Cannot be null or empty.</param>
    public void SetAccessToken(string accessToken)
    {
        _accessToken = accessToken;
    }

    /// <summary>
    /// Initiates the start of a camera with the specified identifier asynchronously.
    /// </summary>
    /// <remarks>This method sends a POST request to the camera API to start the camera. Ensure that the <see
    /// cref="_accessToken"/> is valid and the camera API is accessible.</remarks>
    /// <param name="cameraId">The unique identifier of the camera to start.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the HTTP response message from the
    /// camera API.</returns>
    public async Task<bool> StartCameraAsync(int cameraId)
    {
        using var httpClient = _httpClientFactory.CreateClient("CameraApi");
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

        var request = new HttpRequestMessage(HttpMethod.Post, $"https://localhost:7214/api/camera/startcam/{cameraId}");
        var response = await httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode) return false;
        return true;
    }
}
