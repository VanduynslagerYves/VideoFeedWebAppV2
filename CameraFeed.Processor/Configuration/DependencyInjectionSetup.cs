namespace CameraFeed.Processor.Configuration;

using CameraFeed.Processor.BackgroundServices;
using CameraFeed.Processor.Camera.Worker;
using CameraFeed.Processor.Clients.gRPC;
using CameraFeed.Processor.Repositories;
using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjectionSetup
{
    public static void SetupDependencyInjection(this IServiceCollection services)
    {
        services.AddScoped<IWorkerRepository, WorkerRepository>();

        services.AddSingleton<IObjectDetectionGrpcClient, ObjectDetectionGrpcClient>();
        services.AddSingleton<ICameraWorkerFactory, CameraWorkerFactory>();
        services.AddSingleton<IVideoCaptureFactory, VideoCaptureFactory>();
        services.AddSingleton<IHubConnectionFactory, HubConnectionFactory>();
        services.AddSingleton<IBackgroundSubtractorFactory, BackgroundSubtractorFactory>();

        //services.AddSingleton<CameraWorkerStartupService>(); //Registers the concrete type as a singleton (needed for hosted service resolution).
        //services.AddSingleton<ICameraWorkerStartupService>(sp => sp.GetRequiredService<CameraWorkerStartupService>()); //Allows to inject the interface everywhere else, but both resolve to the same singleton instance.
        //services.AddHostedService(sp => sp.GetRequiredService<CameraWorkerStartupService>()); //Tells the hosted service system to use the singleton CameraWorkerManager instance.
        services.AddHostedService<CameraWorkerStartupService>();
        services.AddSingleton<ICameraWorkerManager, CameraWorkerManager>();
    }
}
