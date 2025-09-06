using CameraFeed.Processor.Data;

namespace CameraFeed.Processor.Repositories;

public interface ICameraRepository
{
}

public class CameraRepository(CamDbContext context) : ICameraRepository
{
}
