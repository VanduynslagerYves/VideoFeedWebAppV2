namespace CameraFeed.SocketServer.Configuration;

using CameraFeed.SocketServer.Hubs;
using CameraFeed.SocketServer.KeyGenerator;
using CameraFeed.SocketServer.Repositories;
using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjectionSetup
{
    public static void SetupDependencyInjection(this IServiceCollection services)
    {
        //Decorate MessageForwarder with MessageForwarderDecorator
        //services.AddSingleton<MessageForwarder>();
        //services.AddSingleton<IMessageForwarder>(provider =>
        //{
        //    var forwarder = provider.GetRequiredService<MessageForwarder>();
        //    return new MessageForwarderDecorator(forwarder);
        //});

        services.AddScoped<IApiKeyRepository, ApiKeyRepository>();
        services.AddSingleton<IApiKeyGenerator, ApiKeyGenerator>();
    }
}
