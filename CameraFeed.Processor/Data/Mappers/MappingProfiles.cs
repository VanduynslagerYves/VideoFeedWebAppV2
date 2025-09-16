using AutoMapper;
using CameraFeed.Processor.Camera.Worker;

namespace CameraFeed.Processor.Data.Mappers;

public class WorkerMappingProfile : Profile
{
    public WorkerMappingProfile()
    {
        CreateMap<WorkerRecord, WorkerOptions>()
            .ForMember(dest => dest.Mode, opt => opt.MapFrom(src => src.UseMotiondetection ? InferenceMode.MotionBased : InferenceMode.Continuous))
            .ForMember(dest => dest.CameraOptions, opt => opt.MapFrom(src => src))
            .ForMember(dest => dest.MotionDetectionOptions, opt => opt.MapFrom(src => src));

        CreateMap<WorkerRecord, CameraProperties>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.CameraId))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Resolution, opt => opt.MapFrom(src => src.Resolution))
            .ForMember(dest => dest.Framerate, opt => opt.MapFrom(src => src.Framerate));

        CreateMap<ResolutionRecord, CameraResolution>();

        CreateMap<WorkerRecord, MotionDetectionOptions>()
            .ForMember(dest => dest.DownscaleFactor, opt => opt.MapFrom(src => src.DownscaleRatio))
            .ForMember(dest => dest.MotionRatio, opt => opt.MapFrom(src => src.MotionRatio));
    }
}