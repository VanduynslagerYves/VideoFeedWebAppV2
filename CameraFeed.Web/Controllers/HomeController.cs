using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using CameraFeed.Web.Models;
using CameraFeed.Web.ApiClients;

namespace CameraFeed.Web.Controllers;

public class HomeController(ICameraApiClient cameraApiClient) : Controller
{
    private readonly ICameraApiClient _cameraApiClient = cameraApiClient;
    private const string ACCESS_TOKEN = "access_token";

    [Authorize(Policy = "AllowedUserOnly")]
    public async Task<IActionResult> Index()
    {
        var accessToken = await HttpContext.GetTokenAsync(ACCESS_TOKEN);
        if (!string.IsNullOrEmpty(accessToken)) _cameraApiClient.SetAccessToken(accessToken);

        var cameraInfoListDTO = await _cameraApiClient.GetActiveCamerasAsync();
        return View(cameraInfoListDTO);
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
