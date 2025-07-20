using CameraFeed.API.Video;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CameraFeed.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CameraController(ICameraWorkerManager cameraWorkerManager, ILogger<CameraController> logger) : ControllerBase
{
    private readonly ICameraWorkerManager _cameraWorkerManager = cameraWorkerManager;
    private readonly ILogger<CameraController> _logger = logger;

    [Authorize]
    [HttpPost("startcam/{cameraId}")]
    public async Task<IActionResult> StartCamera(int cameraId)
    {
        var availableCameraWorkers = await _cameraWorkerManager.GetAvailableCameraWorkersAsync();
        ICameraWorker cameraWorker;

        if(!availableCameraWorkers.TryGetValue(cameraId, out var cameraWorkerValue)) //The cameraworker has not been initialized
        {
            cameraWorker = await _cameraWorkerManager.CreateCameraWorkerAsync(cameraId);
        }
        else
        {
            cameraWorker = cameraWorkerValue.CameraWorker;
        }

        var actionResult = await StartWorker(cameraWorker);
        return actionResult;
    }

    private async Task<IActionResult> StartWorker(ICameraWorker worker)
    {
        var cameraId = worker.CameraId;

        if (worker.IsRunning) return Ok(new { success = false, cameraId, message = "Camera is already running." }); //TODO: Return DTO's

        var taskId = await _cameraWorkerManager.StartCameraWorkerAsync(cameraId);
        if (taskId.HasValue) return Ok(new { success = true, cameraId }); //TODO: Return DTO's

        return BadRequest(new { success = false, cameraId, message = "Failed to start camera." }); //TODO: Return DTO's
    }

    [Authorize]
    [HttpPost("stopcam/{cameraId}")]
    public async Task<IActionResult> StopCamera(int cameraId)
    {
        //TODO: refactor this, we only want the camera(s) to stop when no more clients are conntected to the SignalR hub. Do this in the manager.
        //The manager checks for existing connections for the specified cameraworker through the hubContext.
        var availableCameraWorkers = await _cameraWorkerManager.GetAvailableCameraWorkersAsync();
        if (availableCameraWorkers.TryGetValue(cameraId, out var cameraWorkerInfo))
        {
            if(cameraWorkerInfo.CameraWorker.IsRunning)
            {
                var stopped = await _cameraWorkerManager.StopCameraWorkerAsync(cameraId);
                if (stopped)
                {
                    return Ok(new { success = true, cameraId }); //TODO: Return DTO's
                }
            }

            return BadRequest(new { success = false, cameraId, message = "Failed to stop camera." }); //TODO: Return DTO's
        }

        //TODO: Use BadRequest instead of NotFound to indicate that the camera ID is invalid
        return NotFound(new { success = false, cameraId, message = "Camera not found." }); //TODO: Return DTO's
    }
}
