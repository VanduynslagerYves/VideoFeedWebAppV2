using AutoMapper;
using CameraFeed.Processor.Camera;
using CameraFeed.Processor.Data.Entities;
using CameraFeed.Shared.DTOs;

namespace CameraFeed.Processor.Data.Mappers;

public class WorkerOptionsMappingProfile : Profile
{
    public WorkerOptionsMappingProfile()
    {
        CreateMap<WorkerEntity, WorkerProperties>()
            .ForMember(dest => dest.Mode, opt => opt.MapFrom(src => src.UseMotiondetection ? InferenceMode.MotionBased : InferenceMode.Continuous))
            .ForMember(dest => dest.CameraOptions, opt => opt.MapFrom(src => src))
            .ForMember(dest => dest.MotionDetectionOptions, opt => opt.MapFrom(src => src));

        CreateMap<WorkerEntity, CameraProperties>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.CameraId))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Resolution, opt => opt.MapFrom(src => src.Resolution))
            .ForMember(dest => dest.Framerate, opt => opt.MapFrom(src => src.Framerate));

        CreateMap<ResolutionEntity, CameraResolutionProperties>();

        CreateMap<WorkerEntity, MotionDetectionProperties>()
            .ForMember(dest => dest.DownscaleFactor, opt => opt.MapFrom(src => src.DownscaleRatio))
            .ForMember(dest => dest.MotionRatio, opt => opt.MapFrom(src => src.MotionRatio));
    }
}

public class CameraInfoDtoMappingProfile : Profile
{
    public CameraInfoDtoMappingProfile()
    {
        CreateMap<CameraWorker, CameraInfoDTO>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.CamId))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.CamName))
            .ForMember(dest => dest.Width, opt => opt.MapFrom(src => src.CamWidth))
            .ForMember(dest => dest.Height, opt => opt.MapFrom(src => src.CamHeight));
    }
}