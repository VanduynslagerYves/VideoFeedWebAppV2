using CameraFeed.SocketServer.Hubs;

namespace CameraFeed.SocketServer.Configuration;

public static class HttpSetup
{
    public static void SetupHttp(this WebApplication app, string corsPolicyName)
    {
        app.UseCors(corsPolicyName);

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseHttpsRedirection();

        app.MapControllers();

        app.MapHub<CameraWorkerHub>("/workerhub").RequireAuthorization();
        app.MapHub<FrontendClientHub>("/clienthub").RequireAuthorization();
    }
}
