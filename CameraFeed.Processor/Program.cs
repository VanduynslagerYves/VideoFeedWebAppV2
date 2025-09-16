using CameraFeed.Processor.Camera;
using CameraFeed.Processor.Camera.Worker;
using CameraFeed.Processor.Data;
using CameraFeed.Processor.Data.Mappers;
using CameraFeed.Processor.Repositories;
using CameraFeed.Processor.Services.CameraWorker;
using CameraFeed.Processor.Services.gRPC;
using CameraFeed.Processor.Services.HTTP;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSignalR();
builder.Services.AddHttpClient();

builder.Services.AddDbContext<CamDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CamDb")));
//DI
builder.Services.AddScoped<IWorkerRepository, WorkerRepository>();
builder.Services.AddSingleton<IObjectDetectionGrpcClient, ObjectDetectionGrpcClient>();
builder.Services.AddSingleton<IObjectDetectionHttpClient, ObjectDetectionHttpClient>();
builder.Services.AddSingleton<ICameraWorkerFactory, CameraWorkerFactory>();
builder.Services.AddSingleton<IVideoCaptureFactory, VideoCaptureFactory>();
builder.Services.AddSingleton<IBackgroundSubtractorFactory, BackgroundSubtractorFactory>();

//builder.Services.AddSingleton<IWorkerManager, CameraWorkerManager>();
builder.Services.AddSingleton<CameraWorkerService>(); //Registers the concrete type as a singleton (needed for hosted service resolution).
builder.Services.AddSingleton<IWorkerService>(sp => sp.GetRequiredService<CameraWorkerService>()); //Allows to inject the interface everywhere else, but both resolve to the same singleton instance.
builder.Services.AddHostedService(sp => sp.GetRequiredService<CameraWorkerService>()); //Tells the hosted service system to use the singleton CameraWorkerManager instance.
builder.Services.AddSingleton<ICameraWorkerInitializer, CameraWorkerInitializer>();

builder.Services.AddAutoMapper(cfg => cfg.AddProfile<WorkerMappingProfile>());

builder.WebHost.UseKestrel();

string[] frontendUrls = ["https://katacam-g7fchjfvhucgf8gq.northeurope-01.azurewebsites.net", "https://localhost:7006",
            "https://localhost:44300",
            "https://pure-current-mastodon.ngrok-free.app",
            "https://localhost:4200", //angular client
            "http://localhost:4200"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWeb", policy =>
    {
        policy.WithOrigins(frontendUrls)
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

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<CamDbContext>();
    await DataSeeder.SeedAsync(dbContext);
}

app.Run();
