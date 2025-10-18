namespace CameraFeed.Processor.Configuration;

using CameraFeed.Processor.BackgroundServices;
using CameraFeed.Processor.Camera;
using CameraFeed.Processor.Camera.Factories;
using CameraFeed.Processor.Clients.gRPC;
using CameraFeed.Processor.Clients.SignalR;
using CameraFeed.Processor.Repositories;
using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjectionSetup
{
    public static void SetupDependencyInjection(this IServiceCollection services)
    {
        //Repositories
        services.AddScoped<IWorkerRepository, WorkerRepository>();

        //Clients
        services.AddSingleton<IObjectDetectionGrpcClient, ObjectDetectionGrpcClient>();
        services.AddSingleton<ICameraSignalRclient, CameraSignalRClient>();

        //Factories
        services.AddSingleton<ICameraWorkerFactory, CameraWorkerFactory>();
        services.AddSingleton<IHubConnectionFactory, HubConnectionFactory>();
        services.AddSingleton<IFrameProcessorFactory, FrameProcessorFactory>();
        services.AddSingleton<IVideoCaptureFactory, VideoCaptureFactory>();
        services.AddSingleton<IBackgroundSubtractorFactory, BackgroundSubtractorFactory>();

        //Managers
        services.AddSingleton<ICameraWorkerManager, CameraWorkerManager>();

        //Background Services
        services.AddHostedService<CameraWorkerStartupService>();
    }
}
