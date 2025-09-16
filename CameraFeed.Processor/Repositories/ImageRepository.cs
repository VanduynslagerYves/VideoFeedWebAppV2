using CameraFeed.Processor.Data;
using Microsoft.EntityFrameworkCore;

namespace CameraFeed.Processor.Repositories;

public interface IImageRepository
{
    //Task AddAsync(ImageRecord image);
    //Task<List<ImageRecord>> GetByCameraIdAsync(int cameraId);
}

public class ImageRepository(CamDbContext context) : IImageRepository
{
    //public Task AddAsync(ImageRecord image)
    //{
    //    throw new NotImplementedException();
    //}

    //public Task<List<ImageRecord>> GetByCameraIdAsync(int cameraId)
    //{
    //    throw new NotImplementedException();
    //}
}
