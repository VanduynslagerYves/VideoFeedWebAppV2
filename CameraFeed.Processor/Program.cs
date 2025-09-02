using CameraFeed.Processor.Services.gRPC;
using CameraFeed.Processor.Services.HTTP;
using CameraFeed.Processor.Camera;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using CameraFeed.Processor.Camera.Worker;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSignalR();
builder.Services.AddHttpClient();

//DI
builder.Services.AddSingleton<IObjectDetectionGrpcClient, ObjectDetectionGrpcClient>();
builder.Services.AddSingleton<IObjectDetectionHttpClient, ObjectDetectionHttpClient>();
builder.Services.AddSingleton<IWorkerManager, CameraWorkerManager>();
builder.Services.AddSingleton<ICameraWorkerFactory, CameraWorkerFactory>();
builder.Services.AddSingleton<IVideoCaptureFactory, VideoCaptureFactory>();
builder.Services.AddSingleton<IBackgroundSubtractorFactory, BackgroundSubtractorFactory>();

builder.WebHost.UseKestrel();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWeb", policy =>
    {
        policy.WithOrigins("https://katacam-g7fchjfvhucgf8gq.northeurope-01.azurewebsites.net",
            "https://localhost:7006",
            "https://localhost:44300",
            "https://pure-current-mastodon.ngrok-free.app",
            "https://localhost:4200",
            "http://localhost:4200")//angular client
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
    });
});

//builder.Services.AddCors(options =>
//{
//    options.AddPolicy("AllowAngularClient", policy =>
//    {
//        policy.WithOrigins("http://localhost:4200") // Move these to appsettings.json
//              .AllowAnyMethod()
//              .AllowAnyHeader()
//              .AllowCredentials();
//    });
//});

//Add Authentication Services (validation for JWT tokens)
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.Authority = "https://dev-i4c6oxzfwdlecakx.eu.auth0.com/"; //TODO: get from appsettings
    options.Audience = "https://localhost:7214";
});
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowWeb");

//app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapHub<CameraHub>("/videoHub");

app.MapControllers();

app.Run();
