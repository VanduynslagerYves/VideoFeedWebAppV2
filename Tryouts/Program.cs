using Tryouts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHostedService<LiveProcessingService>();

var app = builder.Build();

app.UseStaticFiles(new StaticFileOptions
{
    ServeUnknownFileTypes = true,
    DefaultContentType = "application/octet-stream"
});

app.MapGet("/", () => Results.Redirect("/stream/index.html"));

await app.RunAsync();