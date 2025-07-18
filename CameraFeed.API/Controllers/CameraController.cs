using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CameraFeed.API.Video;

namespace CameraFeed.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CameraController(ICameraWorkerManager cameraWorkerManager) : ControllerBase
{
    private readonly ICameraWorkerManager _cameraWorkerManager = cameraWorkerManager;

    [Authorize]
    [HttpGet("initcams")]
    public IActionResult InitCameras()
    {
        Console.WriteLine(User.Identity?.Name);
        if (_cameraWorkerManager.GetAvailableCameraWorkers().Count == 0)
        {
            _cameraWorkerManager.CreatecCameraWorkers();
        }

        return Ok(_cameraWorkerManager.GetAvailableCameraWorkers());
    }

    [Authorize]
    [HttpPost("startcam/{cameraId}")]
    public IActionResult StartCamera(int cameraId)
    {
        var availableIds = _cameraWorkerManager.GetAvailableCameraWorkers();
        if(availableIds.Count != 0 && availableIds.Contains(cameraId))
        {
            _cameraWorkerManager.StartCameraWorker(cameraId);
            return Ok(new { success = true, cameraId });
        }

        return NotFound(new { success = false, cameraId });
    }

    [Authorize]
    [HttpPost("stopcam/{cameraId}")]
    public IActionResult StopCamera(int cameraId)
    {
        var availableIds = _cameraWorkerManager.GetAvailableCameraWorkers();
        if (availableIds.Count != 0 && availableIds.Contains(cameraId))
        {
            _cameraWorkerManager.StopCameraWorker(cameraId);
            return Ok(new { success = true, cameraId });
        }

        return NotFound(new { success = false, cameraId });
    }
}
