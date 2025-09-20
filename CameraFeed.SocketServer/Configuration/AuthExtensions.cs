using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace CameraFeed.SocketServer.Configuration;

public static class AuthExtensions
{
    public static void AddAuth(this IServiceCollection services)
    {
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            options.Authority = "https://dev-i4c6oxzfwdlecakx.eu.auth0.com/"; //TODO: get from appsettings
            options.Audience = "https://localhost:7214";
        });

        services.AddAuthorization();
    }
}
