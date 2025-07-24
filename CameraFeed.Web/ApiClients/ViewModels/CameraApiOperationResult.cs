namespace CameraFeed.Web.ApiClients.ViewModels;

using Newtonsoft.Json;

public class CameraApiOperationResult
{
    [JsonProperty("success")]
    public bool Success { get; set; }

    [JsonProperty("message")]
    public required string Message { get; set; }

    [JsonProperty("cameraId")]
    public int CameraId { get; set; }
}
