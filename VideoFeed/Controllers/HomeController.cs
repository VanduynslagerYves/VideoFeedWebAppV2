using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VideoFeed.Models;
using VideoFeed.Video;

namespace VideoFeed.Controllers;

public class HomeController(VideoCaptureService videoCaptureService) : Controller
{
    private readonly VideoCaptureService _videoCaptureService = videoCaptureService;

    [AllowAnonymous]
    public IActionResult Index()
    {
        return View();
    }

    [Authorize]
    public IActionResult StartVideoStream()
    {
        _videoCaptureService.StartCapture();
        return Ok();
    }

    [Authorize]
    public IActionResult StopVideoStream()
    {
        _videoCaptureService.StopCapture();
        return Ok();
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
