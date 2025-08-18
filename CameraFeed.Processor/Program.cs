using CameraFeed.Processor.Services;
using CameraFeed.Processor.Video;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSignalR();
builder.Services.AddHttpClient();

//DI
builder.Services.AddSingleton<IObjectDetectionClient, ObjectDetectionClient>();
//builder.Services.AddSingleton<IObjectDetectionApiClient, ObjectDetectionApiClient>(); //TODO: maybe create a client per worker instead of singleton (without DI)
builder.Services.AddSingleton<ICameraWorkerManager, CameraWorkerManager>();
builder.Services.AddSingleton<ICameraWorkerFactory, CameraWorkerFactory>();
builder.Services.AddSingleton<IVideoCaptureFactory, VideoCaptureFactory>();
builder.Services.AddSingleton<IBackgroundSubtractorFactory, BackgroundSubtractorFactory>();

builder.WebHost.UseKestrel();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWeb", builder =>
    {
        builder.WithOrigins("https://katacam-g7fchjfvhucgf8gq.northeurope-01.azurewebsites.net", "https://localhost:7006", "https://localhost:44300","https://pure-current-mastodon.ngrok-free.app") //Move these to appsettings.json
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
    });
});

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
