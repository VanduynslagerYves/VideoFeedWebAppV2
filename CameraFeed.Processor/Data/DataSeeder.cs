using CameraFeed.Processor.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace CameraFeed.Processor.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(CamDbContext context)
    {
        // Ensure database is created
        await context.Database.EnsureCreatedAsync();

        // Check if data already exists
        if (await context.WorkerRecords.AnyAsync())
            return;

        // Create resolutions
        var res1 = new ResolutionDbModel("1080p", 1920, 1080);
        var res2 = new ResolutionDbModel("720p", 1280, 720);

        // Add resolutions
        context.Set<ResolutionDbModel>().AddRange(res1, res2);

        // Create workers
        var worker1 = new WorkerDbModel
        {
            Id = Guid.NewGuid(),
            Name = "Camera 1",
            CameraId = 0,
            Enabled = true,
            Framerate = 15,
            UseMotiondetection = true,
            DownscaleRatio = 16,
            MotionRatio = 0.005,
            ResolutionId = res1.Id,
            Resolution = res1
        };

        var worker2 = new WorkerDbModel
        {
            Id = Guid.NewGuid(),
            Name = "Camera 2",
            CameraId = 1,
            Enabled = true,
            Framerate = 15,
            UseMotiondetection = true,
            DownscaleRatio = 16,
            MotionRatio = 0.005,
            ResolutionId = res2.Id,
            Resolution = res2
        };

        context.WorkerRecords.AddRange(worker1, worker2);

        await context.SaveChangesAsync();
    }
}