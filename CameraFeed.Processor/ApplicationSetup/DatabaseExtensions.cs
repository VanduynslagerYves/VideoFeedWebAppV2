using CameraFeed.Processor.Data;
using Microsoft.EntityFrameworkCore;

namespace CameraFeed.Processor.ApplicationSetup;

public static class DatabaseExtensions
{
    public static void AddDatabase(this IServiceCollection services, string? dbName)
    {
        services.AddDbContext<CamDbContext>(options =>
            options.UseSqlServer(dbName));
    }
}
