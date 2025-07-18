using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Net.Http.Headers;
using VideoFeed.Models;

namespace VideoFeed.Controllers;

public class HomeController(IHttpClientFactory httpClientFactory, IConfiguration config) : Controller
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    [Authorize]
    public async Task<IActionResult> Index()
    {
        using var httpClient = _httpClientFactory.CreateClient();
        var accessToken = await HttpContext.GetTokenAsync("access_token");
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var initResponse = await InitCams(httpClient);
        if(!initResponse.IsSuccessStatusCode)
        {
            return StatusCode((int)initResponse.StatusCode, "Error while initializing cameras");
        }

        var startResponse = await StartCam(httpClient, 0);
        if (!startResponse.IsSuccessStatusCode)
        {
            return StatusCode((int)startResponse.StatusCode, "Error while starting camera(s)");
        }

        var start1Response = await StartCam(httpClient, 1);
        if (!start1Response.IsSuccessStatusCode)
        {
            return StatusCode((int)start1Response.StatusCode, "Error while starting camera(s)");
        }

        var content = await startResponse.Content.ReadAsStringAsync();
        return View(model: content);
    }

    private async static Task<HttpResponseMessage> InitCams(HttpClient client)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"https://localhost:7214/api/camera/initcams");
        var response = await client.SendAsync(request);

        return response;
    }

    private async static Task<HttpResponseMessage> StartCam(HttpClient client, int cameraId)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"https://localhost:7214/api/camera/startcam/{cameraId}");
        var response = await client.SendAsync(request);

        return response;
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
