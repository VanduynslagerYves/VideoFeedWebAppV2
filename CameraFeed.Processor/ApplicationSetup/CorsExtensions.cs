namespace CameraFeed.Processor.ApplicationSetup;

public static class CorsExtensions
{
    static readonly string[] _frontendUrls = ["https://katacam-g7fchjfvhucgf8gq.northeurope-01.azurewebsites.net", "https://localhost:7006",
            "https://localhost:44300",
            "https://pure-current-mastodon.ngrok-free.app",
            "https://localhost:4200", //angular client
            "http://localhost:4200"];

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