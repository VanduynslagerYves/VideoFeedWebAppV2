using CameraFeed.Processor.Camera;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace CameraFeed.Processor.Controllers;

[Route("api/camera")]
[ApiController]
public class CameraController(IWorkerManager cameraWorkerManager, IHubContext<CameraHub> hubContext, ILogger<CameraController> logger) : ControllerBase
{
    private readonly IWorkerManager _cameraWorkerManager = cameraWorkerManager; //singleton
    private readonly IHubContext<CameraHub> _hubContext = hubContext;
    private readonly ILogger<CameraController> _logger = logger;

    [Authorize]
    [HttpPost("startcam/{cameraId}")]
    public async Task<IActionResult> StartCamera(int cameraId) //Add DTO for cameraId and cameraOptions and pass it down the line
    {
        var selectedResolution = "720p"; //TODO: get from viewmodel or DTO
        var selectedFramerate = 15; //TODO: get from viewmodel or DTO

        var optioons = new WorkerOptions //TODO: get from viewmodel or DTO
        {
            CameraId = cameraId,
            Mode = InferenceMode.MotionBased,
            CameraOptions = new CameraOptions
            {
                Resolution = SupportedCameraProperties.GetResolutionById(selectedResolution),
                Framerate = selectedFramerate,
            }
        };

        var result = await _cameraWorkerManager.StartAsync(optioons);
        return result;
    }

    //[AllowAnonymous]
    //[HttpPost("person-detected")]
    //public async Task<IActionResult> PersonDetected([FromBody] PersonDetectedDto dto)
    //{
    //    var NotifyHumanDetectedGroup = $"camera_{dto.CameraId}_human_detected";

    //    //Do stuff
    //    //await _hubContext.Clients.Group(NotifyHumanDetectedGroup).SendAsync("HumanDetected", dto.CameraId.ToString());

    //    return Ok();
    //}

    [Authorize]
    [HttpPost("stopcam/{cameraId}")]
    public async Task<IActionResult> StopCamera(int cameraId)
    {
        await _cameraWorkerManager.StopAsync(cameraId);
        return Ok();
    }
}

public class PersonDetectedDto
{
    public required string CameraId { get; set; }
}
