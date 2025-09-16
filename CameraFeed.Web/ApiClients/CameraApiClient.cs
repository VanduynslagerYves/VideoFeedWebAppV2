using CameraFeed.Shared.DTO;
using CameraFeed.Web.ApiClients.ViewModels;
using Newtonsoft.Json;
using System.Net.Http.Headers;

namespace CameraFeed.Web.ApiClients;

public interface ICameraApiClient
{
    [Obsolete("Use GetAvailableCameraIdsAsync instead")]
    Task<CameraApiOperationResult?> StartCameraAsync(int cameraId);
    Task<List<CameraInfoDTO>?> GetActiveCamerasAsync();
    void SetAccessToken(string accessToken);
}

public abstract class CameraApiClientBase(IHttpClientFactory httpClientFactory) : ICameraApiClient
{
    protected readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    protected string? _accessToken;

    [Obsolete("Use GetAvailableCameraIdsAsync instead")]
    public abstract Task<CameraApiOperationResult?> StartCameraAsync(int cameraId);

    public abstract Task<List<CameraInfoDTO>?> GetActiveCamerasAsync();

    public void SetAccessToken(string accessToken)
    {
        _accessToken = accessToken;
    }
}

public class CameraApiClient(IHttpClientFactory httpClientFactory, ILogger<CameraApiClient> _logger) : CameraApiClientBase(httpClientFactory)
{
    protected readonly ILogger<CameraApiClient> _logger = _logger;

    [Obsolete("Use GetAvailableCameraIdsAsync instead")]
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

    public override async Task<List<CameraInfoDTO>?> GetActiveCamerasAsync()
    {
        using var httpClient = _httpClientFactory.CreateClient("CameraApi");
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

        var request = new HttpRequestMessage(HttpMethod.Get, $"https://localhost:7214/api/camera/active/");

        try
        {
            var response = await httpClient.SendAsync(request);

            var responseString = await response.Content.ReadAsStringAsync();
            var responseObject = JsonConvert.DeserializeObject<List<CameraInfoDTO>>(responseString);

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
            _logger.LogError(ex, "HTTP request failed");
            return null;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "HTTP request timed out");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error");
            return null;
        }
    }
}
