using CameraFeed.Processor.Data;
using CameraFeed.Processor.Configuration;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
//builder.Services.AddSignalR();
builder.Services.AddHttpClient();

var connectionString = builder.Configuration.GetConnectionString("CamDb");
builder.Services.AddDatabase(connectionString);

builder.Services.AddProcessorServices();
builder.Services.AddProcessorAutoMapperConfig();
//var corsPolicyName = builder.Services.AddFrontendCors();

builder.WebHost.UseKestrel();

builder.Services.AddAuth();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

//app.UseCors(corsPolicyName);

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

//app.MapHub<CameraHub>("/videoHub");

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    //Database seed for development
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<CamDbContext>();
    await DataSeeder.SeedAsync(dbContext);
}

app.Run();
