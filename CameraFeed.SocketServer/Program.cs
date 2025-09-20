using CameraFeed.SocketServer.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.SetupDependencyInjection();

builder.Services.AddControllers();
builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 200 * 1024; //200 KB
    options.EnableDetailedErrors = true;
});

var corsPolicyName = builder.Services.AddFrontendCors();

builder.WebHost.UseKestrel();
builder.Services.AddAuth();

var app = builder.Build();

app.SetupHttp(corsPolicyName);

app.Run();
