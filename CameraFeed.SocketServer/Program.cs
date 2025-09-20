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

var connectionString = builder.Configuration.GetConnectionString("ApiKeyDb");
builder.Services.AddDatabase(connectionString);

var corsPolicyName = builder.Services.AddFrontendCors();

builder.WebHost.UseKestrel();
builder.Services.AddAuth();

var app = builder.Build();

app.SetupHttp(corsPolicyName);

app.Run();
