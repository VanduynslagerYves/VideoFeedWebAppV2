using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CameraFeed.API.Video;

namespace CameraFeed.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CameraController(ICameraWorkerManager cameraWorkerManager, ILogger<CameraController> logger) : ControllerBase
{
    private readonly ICameraWorkerManager _cameraWorkerManager = cameraWorkerManager;
    private readonly ILogger<CameraController> _logger = logger;

    [Authorize]
    [HttpGet("initcams")]
    public async Task<IActionResult> InitCameras()
    {
        _logger.LogInformation(message: $"Authenticated: {User.Identity?.IsAuthenticated}");

        var availableWorkers = await _cameraWorkerManager.GetAvailableCameraWorkersAsync();
        if (availableWorkers.Count == 0)
        {
            await _cameraWorkerManager.CreatecCameraWorkersAsync();
            availableWorkers = await _cameraWorkerManager.GetAvailableCameraWorkersAsync();
        }

        return Ok(availableWorkers);
    }

    [Authorize]
    [HttpPost("startcam/{cameraId}")]
    public async Task<IActionResult> StartCamera(int cameraId)
    {
        var availableIds = await _cameraWorkerManager.GetAvailableCameraWorkersAsync();
        if (availableIds.Contains(cameraId))
        {
            var result = await _cameraWorkerManager.StartCameraWorkerAsync(cameraId);
            if (result.HasValue)
            {
                return Ok(new { success = true, cameraId }); //TODO: Return DTO's
            }
            return BadRequest(new { success = false, cameraId, message = "Failed to start camera." }); //TODO: Return DTO's
        }

        //TODO: Use BadRequest instead of NotFound to indicate that the camera ID is invalid
        return NotFound(new { success = false, cameraId, message = "Camera not found." }); //TODO: Return DTO's
    }

    [Authorize]
    [HttpPost("stopcam/{cameraId}")]
    public async Task<IActionResult> StopCamera(int cameraId)
    {
        var availableIds = await _cameraWorkerManager.GetAvailableCameraWorkersAsync();
        if (availableIds.Contains(cameraId))
        {
            var stopped = await _cameraWorkerManager.StopCameraWorkerAsync(cameraId);
            if (stopped)
            {
                return Ok(new { success = true, cameraId }); //TODO: Return DTO's
            }
            return BadRequest(new { success = false, cameraId, message = "Failed to stop camera." }); //TODO: Return DTO's
        }

        //TODO: Use BadRequest instead of NotFound to indicate that the camera ID is invalid
        return NotFound(new { success = false, cameraId, message = "Camera not found." }); //TODO: Return DTO's
    }
}
