using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace CameraFeed.SocketServer.Configuration;

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
            options.Authority = "https://dev-i4c6oxzfwdlecakx.eu.auth0.com/";
            options.Audience = "https://localhost:7244";

            // Recommended: Validate issuer, audience, and token lifetime
            options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = "https://dev-i4c6oxzfwdlecakx.eu.auth0.com/",
                ValidateAudience = true,
                ValidAudience = "https://localhost:7244",
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true
            };
        });

        services.AddAuthorization();
    }
}
