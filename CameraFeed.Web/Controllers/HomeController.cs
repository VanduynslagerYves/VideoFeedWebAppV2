using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using CameraFeed.Web.Models;
using CameraFeed.Web.ApiClients;
using CameraFeed.Web.ViewModels;

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

        var selectedCameraIds = new List<int> { 0, 1, 2 }; //TODO: Get selected cameras from user selection, 2 does not exist, so it will not start
        var cameraRequestViewModel = new CameraRequestViewModel();

        var availableCameraIds = await _cameraApiClient.GetAvailableCamerasAsync();
        if(availableCameraIds == null || availableCameraIds.Count == 0) return View(cameraRequestViewModel); //No cameras available, return view with empty lists

        foreach (var cameraId in selectedCameraIds)
        {
            if (!availableCameraIds.Contains(cameraId)) continue; //skip this camera if it is not available

            var result = await _cameraApiClient.StartCameraAsync(cameraId);
            if (result == null) //something went wrong if result is null
            {
                cameraRequestViewModel.FailList.Add(cameraId);
                continue;
            }

            if (result.Success)
            {
                cameraRequestViewModel.SuccessList.Add(cameraId); //we should also send the message in the result to the viewmodel
            }
            else
            {
                cameraRequestViewModel.FailList.Add(cameraId);
            }
        }

        return View(cameraRequestViewModel);
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
