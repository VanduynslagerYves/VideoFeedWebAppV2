using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using VideoFeed.Models;
using VideoFeed.Video;

namespace VideoFeed.Controllers;

public class HomeController(VideoCaptureService videoCaptureService) : Controller
{
    private readonly VideoCaptureService _videoCaptureService = videoCaptureService;

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult StartVideoStream()
    {
        _videoCaptureService.StartCapture();
        return Ok();
    }

    public IActionResult StopVideoStream()
    {
        _videoCaptureService.StopCapture();
        return Ok();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
