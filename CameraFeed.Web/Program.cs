using CameraFeed.Web.ApiClients;
using CameraFeed.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Read authentication settings from appsettings
var authSettings = builder.Configuration.GetSection("Authentication");

// Add services to the container.
builder.Services.AddControllersWithViews();

//DI
builder.Services.AddScoped<ICameraApiClient, CameraApiClient>();
builder.Services.AddSingleton<IAllowedUsersService, AllowedUsersService>();

//builder.WebHost.UseKestrel();
builder.WebHost.UseIISIntegration(); //Absolutely necessary for deployment as an Azure App Service

builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
}).AddCookie().AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    options.Authority = authSettings["Authority"]; //These can be added as environment variables like Authentication__Authority etc. Like this there's no need for exposing secrets in appsettings.json
    options.ClientId = authSettings["ClientId"];
    options.ClientSecret = authSettings["ClientSecret"];

    options.ResponseType = "code"; // Use "code" for authorization code flow
    options.SaveTokens = true;

    options.Scope.Clear();
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email"); // Add necessary scopes

    options.CallbackPath = "/signin-oidc"; // Default callback path
    options.SignedOutCallbackPath = "/signout-callback-oidc";

    options.SaveTokens = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        NameClaimType = "name",
        RoleClaimType = "role"
    };

    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;

    options.Events = new OpenIdConnectEvents
    {
        OnRedirectToIdentityProviderForSignOut = context =>
        {
            var logoutUri = $"{options.Authority}/v2/logout?client_id={options.ClientId}";

            var postLogoutUri = context.Properties.RedirectUri;
            if (!string.IsNullOrEmpty(postLogoutUri))
            {
                logoutUri += $"&returnTo={Uri.EscapeDataString(postLogoutUri)}";
            }

            context.Response.Redirect(logoutUri);
            context.HandleResponse();

            return Task.CompletedTask;
        },
        OnRedirectToIdentityProvider = context =>
        {
            var apiBaseUrl = builder.Configuration["ApiBaseUrl"];
            context.ProtocolMessage.SetParameter("audience", apiBaseUrl);
            return Task.CompletedTask;
        }
    };
});


//TODO: figure out how to use Auth0 roles
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AllowedUserOnly", policy =>
        policy.RequireAssertion(context =>
        {
            var httpContextAccessor = context.Resource as IHttpContextAccessor
                ?? (context.Resource as HttpContext)?.RequestServices.GetService<IHttpContextAccessor>();

var httpContext = httpContextAccessor?.HttpContext
    ?? context.Resource as HttpContext;

var allowedUsersService = httpContext?.RequestServices.GetService<IAllowedUsersService>();

var accountId = context.User.Identity?.Name;
return accountId != null && allowedUsersService?.IsAllowed(accountId) == true;
        }));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

//app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
