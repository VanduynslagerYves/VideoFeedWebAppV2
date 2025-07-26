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

        var selectedCamIds = new List<int> { 0, 1 }; //TODO: Get selected cameras from user selection
        var cameraRequestViewModel = new CameraRequestViewModel();

        foreach (var cameraSelectedId in selectedCamIds)
        {
            var result = await _cameraApiClient.StartCameraAsync(cameraSelectedId);
            if (result == null) //something went wrong if result is null
            {
                cameraRequestViewModel.FailList.Add(cameraSelectedId);
                continue;
            }

            if (result.Success)
            {
                cameraRequestViewModel.SuccessList.Add(cameraSelectedId); //we should also send the message in the result to the viewmodel
            }
            else
            {
                cameraRequestViewModel.FailList.Add(cameraSelectedId);
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
