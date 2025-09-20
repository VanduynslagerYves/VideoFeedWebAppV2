using CameraFeed.SocketServer.Configuration;
using CameraFeed.SocketServer.Hubs;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<MessageForwarder>();

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

// Configure the HTTP request pipeline.
app.UseCors(corsPolicyName);

app.UseWebSockets();

app.UseHttpsRedirection();

app.UseAuthorization();
app.UseAuthorization();

app.MapControllers();

app.MapHub<CameraWorkerHub>("/receiverhub");
app.MapHub<FrontendClientHub>("/forwarderhub");

var forwarder = app.Services.GetRequiredService<MessageForwarder>();
var forwarderHubContext = app.Services.GetRequiredService<IHubContext<FrontendClientHub>>();
forwarder.SetHubContext(forwarderHubContext);

app.Run();
