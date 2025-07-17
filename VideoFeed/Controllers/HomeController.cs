using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VideoFeed.Models;
using VideoFeed.Video;

namespace VideoFeed.Controllers;

public class HomeController(ICameraWorkerManager cameraWorkerManager) : Controller
{
    private readonly ICameraWorkerManager _cameraWorkerManager = cameraWorkerManager;

    [Authorize]
    public IActionResult Index()
    {
        _cameraWorkerManager.CreateWorkers();
        var workerKeys = _cameraWorkerManager.GetRunningWorkers();

        List<int> camerasSelected = [0, 1];
        foreach (var cameraSelected in camerasSelected)
        {
            if (workerKeys != null && workerKeys.Contains(cameraSelected))
            {
                _cameraWorkerManager.StartCameraWorker(cameraSelected);
            }
        }

        return View();
    }

    [AllowAnonymous]
    public IActionResult Privacy()
    {
        return View();
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
