using CameraAPI.Video;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSignalR();

builder.Services.AddSingleton<ICameraWorkerManager, CameraWorkerManager>();
builder.Services.AddSingleton<ICameraWorkerFactory, CameraWorkerFactory>();

builder.WebHost.UseKestrel();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWeb", builder =>
    {
        builder.WithOrigins("https://localhost:7006") // Or specify specific origins like "http://192.168.x.x"
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
    });
});

// 1. Add Authentication Services
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

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapHub<CameraHub>("/videoHub");

app.MapControllers();

app.Run();
