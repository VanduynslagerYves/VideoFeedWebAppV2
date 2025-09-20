using CameraFeed.Processor.Data.Mappers;

namespace CameraFeed.Processor.Configuration;
public static class AutoMapperSetup
{
    public static void AddProcessorAutoMapperConfig(this IServiceCollection services)
    {
        services.AddAutoMapper(cfg => cfg.AddProfile<WorkerOptionsMappingProfile>());
        services.AddAutoMapper(cfg => cfg.AddProfile<CameraInfoDtoMappingProfile>());
    }
}
