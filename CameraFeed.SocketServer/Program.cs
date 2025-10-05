using CameraFeed.SocketServer.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add environment-specific appsettings
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

// Add services to the container.
builder.Services.SetupDependencyInjection();

builder.Services.AddControllers();
builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 300 * 1024; //200 KB for image + 100Kb for overhead
    options.EnableDetailedErrors = true;
}).AddMessagePackProtocol();

var connectionString = builder.Configuration.GetConnectionString("ApiKeyDb");
builder.Services.AddDatabase(connectionString);

var corsPolicyName = builder.Services.AddFrontendCors();

builder.WebHost.UseKestrel();
builder.Services.AddAuth();

var app = builder.Build();

app.SetupHttp(corsPolicyName);

app.Run();
