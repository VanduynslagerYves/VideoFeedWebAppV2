using CameraFeed.API.DTO;
using CameraFeed.API.Video;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace CameraFeed.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CameraController(ICameraWorkerManager cameraWorkerManager, IHubContext<CameraHub> hubContext, ILogger<CameraController> logger) : ControllerBase
{
    // Injected as singleton
    private readonly ICameraWorkerManager _cameraWorkerManager = cameraWorkerManager;
    private readonly IHubContext<CameraHub> _hubContext = hubContext;
    private readonly ILogger<CameraController> _logger = logger;

    [Authorize]
    [HttpPost("startcam/{cameraId}")]
    public async Task<IActionResult> StartCamera(int cameraId) //Add DTO for cameraId and cameraOptions and pass it down the line
    {
        var selectedResolution = "720p"; //TODO: get from viewmodel or DTO
        var selectedFramerate = 15; //TODO: get from viewmodel or DTO

        var cameraWorkerOptions = new CameraWorkerOptions //TODO: get from viewmodel or DTO
        {
            CameraId = cameraId,
            UseMotionDetection = false,
            UseContinuousInference = true,
            CameraOptions = new CameraOptions
            {
                Resolution = SupportedCameraProperties.GetResolutionById(selectedResolution),
                Framerate = selectedFramerate,
            }
        };

        var availableCameraWorkers = await _cameraWorkerManager.GetAvailableCameraWorkersAsync();
        ICameraWorker cameraWorker;

        if (!availableCameraWorkers.TryGetValue(cameraId, out var cameraWorkerValue)) //The cameraworker has not yet been initialized
        {
            cameraWorker = await _cameraWorkerManager.CreateCameraWorkerAsync(cameraWorkerOptions);
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

        if (worker.IsRunning) return CameraOperationResultFactory.Create(cameraId, ResponseMessages.CameraAlreadyRunning);

        var taskId = await _cameraWorkerManager.StartCameraWorkerAsync(cameraId);
        if (taskId.HasValue) return CameraOperationResultFactory.Create(cameraId, ResponseMessages.CameraStarted);

        return CameraOperationResultFactory.Create(cameraId, ResponseMessages.CameraStartFailed);
    }

    [AllowAnonymous]
    [HttpPost("person-detected")]
    public async Task<IActionResult> PersonDetected([FromBody] PersonDetectedDto dto)
    {
        var NotifyHumanDetectedGroup = $"camera_{dto.CameraId}_human_detected";

        //Do stuff
        //await _hubContext.Clients.Group(NotifyHumanDetectedGroup).SendAsync("HumanDetected", dto.CameraId.ToString());

        return Ok();
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
            if (cameraWorkerInfo.CameraWorker.IsRunning)
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

public class PersonDetectedDto
{
    public required string CameraId { get; set; }
}
