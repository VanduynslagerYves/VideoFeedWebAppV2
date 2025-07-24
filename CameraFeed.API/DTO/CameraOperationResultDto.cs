namespace CameraFeed.API.DTO;

public class CameraOperationResultDto
{
    public bool Success { get; set; }
    public required string Message { get; set; }
    public int CameraId { get; set; }
}
