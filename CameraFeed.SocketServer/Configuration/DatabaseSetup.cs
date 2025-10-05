using CameraFeed.SocketServer.Data;
using Microsoft.EntityFrameworkCore;

namespace CameraFeed.SocketServer.Configuration;

public static class DatabaseSetup
{
    public static void AddDatabase(this IServiceCollection services, string? dbName)
    {
        services.AddDbContext<ApiKeyDbContext>(options =>
            options.UseSqlServer(dbName));
    }
}
