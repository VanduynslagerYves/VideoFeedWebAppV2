using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CameraFeed.Web.Controllers;

[Route("api/signalr")]
[ApiController]
public class SignalRController : Controller
{
    [Authorize]
    [HttpGet("connection-info")]
    public async Task<IActionResult> GetConnectionInfo()
    {
        // Retrieve the access token from the authentication cookie
        var accessToken = await HttpContext.GetTokenAsync("access_token");

        // Optionally, generate a short-lived token here

        return Ok(accessToken);
    }
}
