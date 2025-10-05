namespace CameraFeed.Processor.Configuration;

public static class CorsSetup
{
    static readonly string[] _frontendUrls = [
        "https://localhost:7006", //razor https client (can be deleted)
        "https://localhost:4200", //angular https client
        "http://localhost:4200", //angular http client
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