using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace CameraFeed.Processor.Configuration;

public static class AuthSetup
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

            // Recommended: Validate issuer, audience, and token lifetime
            options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = "https://dev-i4c6oxzfwdlecakx.eu.auth0.com/",
                ValidateAudience = true,
                ValidAudience = "https://localhost:7214",
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true
            };
        });

        services.AddAuthorization();
    }
}
