using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using VideoFeed.Video;

var builder = WebApplication.CreateBuilder(args);

// Read authentication settings from appsettings
var authSettings = builder.Configuration.GetSection("Authentication");

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();//.AddMessagePackProtocol();
builder.Services.AddHostedService<CameraService>();

builder.WebHost.UseKestrel();
builder.WebHost.ConfigureKestrel((context, options) =>
{
    options.Configure(context.Configuration.GetSection("Kestrel"));
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()  // Or specify specific origins like "http://192.168.x.x"
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
}).AddCookie().AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    options.Authority = authSettings["Authority"];
    options.ClientId = authSettings["ClientId"]; //TODO: remove these from appsettings
    options.ClientSecret = authSettings["ClientSecret"]; //TODO: remove these from appsettings
    options.ResponseType = "code"; // Use "code" for authorization code flow
    options.SaveTokens = true;

    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email"); // Add necessary scopes

    options.CallbackPath = "/signin-oidc"; // Default callback path
    options.SignedOutCallbackPath = "/signout-callback-oidc"; // Ensure it's properly set

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
        }
    };
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapHub<VideoHub>("/videoHub");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
