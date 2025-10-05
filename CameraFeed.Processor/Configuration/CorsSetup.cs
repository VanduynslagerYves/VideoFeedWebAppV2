namespace CameraFeed.Processor.Configuration;

public static class CorsSetup
{
    static readonly string[] _frontendUrls = [
        "https://localhost:7006",
        "https://localhost:4200", //angular client
        "http://localhost:4200",
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