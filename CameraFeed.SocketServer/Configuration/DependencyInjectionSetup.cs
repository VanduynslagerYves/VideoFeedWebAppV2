using CameraFeed.SocketServer.Hubs;
using CameraFeed.SocketServer.Repositories;

namespace CameraFeed.SocketServer.Configuration;

public static class DependencyInjectionSetup
{
    public static void SetupDependencyInjection(this IServiceCollection services)
    {
        services.AddSingleton<IFrontendForwarder, FrontendForwarder>();
        services.AddSingleton<IBackendForwarder, BackendForwarder>();

        services.AddScoped<IApiKeyRepository, ApiKeyRepository>();
    }
}
