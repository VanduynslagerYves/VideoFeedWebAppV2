using CameraFeed.SocketServer.Hubs;

namespace CameraFeed.SocketServer.Configuration;

public static class HttpSetup
{
    public static void SetupHttp(this WebApplication app, string corsPolicyName)
    {
        app.UseCors(corsPolicyName);

        app.UseWebSockets();

        app.UseHttpsRedirection();

        app.UseAuthorization();
        app.UseAuthorization();

        app.MapControllers();

        app.MapHub<CameraWorkerHub>("/receiverhub");
        //app.MapHub<FrontendClientHub>("/forwarderhub");
    }
}
