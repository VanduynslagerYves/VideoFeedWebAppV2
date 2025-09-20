using CameraFeed.Processor.Data;
using Microsoft.EntityFrameworkCore;

namespace CameraFeed.Processor.Configuration;

public static class DatabaseSetup
{
    public static void AddDatabase(this IServiceCollection services, string? dbName)
    {
        services.AddDbContext<CamDbContext>(options =>
            options.UseSqlServer(dbName));
    }
}
