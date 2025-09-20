namespace CameraFeed.SocketServer.Configuration;

public static class CorsExtensions
{
    static readonly string[] _frontendUrls = [
        "https://localhost:7006", //razor https client
        "https://localhost:4200", //angular https client
        "http://localhost:4200", //angular http client
        //"https://katacam-g7fchjfvhucgf8gq.northeurope-01.azurewebsites.net",
        //"https://localhost:44300",
        //"https://pure-current-mastodon.ngrok-free.app"
        ];

    static readonly string _policyName = "AllowFrontend";

    public static string AddFrontendCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(_policyName, policy =>
            {
                policy.WithOrigins(_frontendUrls)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            });
        });

        return _policyName;
    }
}