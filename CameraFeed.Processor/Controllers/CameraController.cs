using CameraFeed.Processor.Repositories;
using CameraFeed.Processor.Camera.Worker;
using CameraFeed.Shared.DTOs;
using CameraFeed.Processor.Clients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace CameraFeed.Processor.Controllers;

[Route("api/camera")]
[ApiController]
public class CameraController(ICameraWorkerManager cameraWorkerManager, IWorkerRepository workerRepository, IHubContext<CameraHub> hubContext) : ControllerBase
{
    private readonly ICameraWorkerManager _cameraWorkerManager = cameraWorkerManager; //singleton
    private readonly IWorkerRepository _workerRepository = workerRepository; //scoped
    private readonly IHubContext<CameraHub> _hubContext = hubContext;
    //private readonly IMapper _mapper = mapper;

    [Authorize]
    [HttpGet("active")]
    public List<CameraInfoDTO> GetActiveCameras()
    {
        //TODO: inject CameraWorkerManager instead of WorkerService and get active cameras from there
        return [.. _cameraWorkerManager.GetWorkerDtos(isActive: true)];
    }

    [AllowAnonymous]
    [HttpPost("person-detected")]
    public async Task<IActionResult> PersonDetected([FromBody] PersonDetectedDto dto)
    {
        var NotifyHumanDetectedGroup = $"camera_{dto.CameraId}_human_detected";
        await _hubContext.Clients.Group(NotifyHumanDetectedGroup).SendAsync("HumanDetected", dto.CameraId.ToString());
        return Ok();
    }

    //[Authorize]
    //[HttpPost("startcam/{cameraId}")]
    //public async Task<IActionResult> StartCameraAsync(int cameraId) //Add DTO for cameraId and cameraOptions and pass it down the line
    //{
    //    var selectedResolution = "720p"; //TODO: get from viewmodel or DTO
    //    var selectedFramerate = 15; //TODO: get from viewmodel or DTO

    //    var optioons = new WorkerOptions //TODO: get from viewmodel or DTO
    //    {
    //        CameraId = cameraId,
    //        CameraName = $"Camera {cameraId}",
    //        Mode = InferenceMode.MotionBased,
    //        CameraOptions = new CameraOptions
    //        {
    //            Resolution = SupportedCameraProperties.GetResolutionById(selectedResolution),
    //            Framerate = selectedFramerate,
    //        },
    //        MotionDetectionOptions = new MotionDetectionOptions
    //        {
    //            DownscaleFactor = 16,
    //            MotionRatio = 0.005,
    //        }
    //    };

    //    var result = await _cameraWorkerManager.StartAsync(optioons);
    //    return result;
    //}

    //[Authorize]
    //[HttpPost("stopcam/{cameraId}")]
    //public async Task<IActionResult> StopCamera(int cameraId)
    //{
    //    await _cameraWorkerManager.StopAsync(cameraId);
    //    return Ok();
    //}
}

public class PersonDetectedDto
{
    public required string CameraId { get; set; }
}
