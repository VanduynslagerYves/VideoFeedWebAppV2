using CameraFeed.Processor.Data.Mappers;

namespace CameraFeed.Processor.ApplicationSetup;
public static class AutoMapperExtensions
{
    public static void AddProcessorAutoMapperConfig(this IServiceCollection services)
    {
        services.AddAutoMapper(cfg => cfg.AddProfile<WorkerOptionsMappingProfile>());
        services.AddAutoMapper(cfg => cfg.AddProfile<CameraInfoDtoMappingProfile>());
    }
}
