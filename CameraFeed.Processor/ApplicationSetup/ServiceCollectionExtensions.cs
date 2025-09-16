namespace CameraFeed.Processor.ApplicationSetup;

using CameraFeed.Processor.Camera.Worker;
using CameraFeed.Processor.Clients.gRPC;
using CameraFeed.Processor.Repositories;
using Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static void AddProcessorServices(this IServiceCollection services)
    {
        services.AddScoped<IWorkerRepository, WorkerRepository>();

        services.AddSingleton<IObjectDetectionGrpcClient, ObjectDetectionGrpcClient>();
        services.AddSingleton<ICameraWorkerFactory, CameraWorkerFactory>();
        services.AddSingleton<IVideoCaptureFactory, VideoCaptureFactory>();
        services.AddSingleton<IBackgroundSubtractorFactory, BackgroundSubtractorFactory>();

        services.AddSingleton<CameraWorkerService>(); //Registers the concrete type as a singleton (needed for hosted service resolution).
        services.AddSingleton<IWorkerService>(sp => sp.GetRequiredService<CameraWorkerService>()); //Allows to inject the interface everywhere else, but both resolve to the same singleton instance.
        services.AddHostedService(sp => sp.GetRequiredService<CameraWorkerService>()); //Tells the hosted service system to use the singleton CameraWorkerManager instance.
        services.AddSingleton<ICameraWorkerManager, CameraWorkerManager>();
    }
}
