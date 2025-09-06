using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CameraFeed.Processor.Data;

public class CamDbContext(DbContextOptions<CamDbContext> options) : DbContext(options)
{
    public DbSet<WorkerRecord> CameraRecords { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkerRecord>(MapWorkerRecord);
        modelBuilder.Entity<ImageRecord>(MapImageRecord);
    }

    protected static void MapWorkerRecord(EntityTypeBuilder<WorkerRecord> recordBuilder)
    {
        recordBuilder.ToTable("camera").HasKey(x => x.Id);
    }

    protected static void MapImageRecord(EntityTypeBuilder<ImageRecord> recordBuilder)
    {
        recordBuilder.ToTable("image").HasKey(x => x.Id);
        recordBuilder.Property(x => x.CameraId);
        recordBuilder.Property(x => x.EventTime);
        recordBuilder.Property(x => x.ImageData);
    }
}

public class WorkerRecord
{
    public Guid Id { get; set; }
    public bool UseMotiondetection { get; set; }
    public required CameraRecord Camera { get; set; }
}

public record CameraRecord
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int Framerate { get; set; }
    public int DownscaleRatio { get; set; }
    public double MotionRatio { get; set; }
    public required Resolution Resolution { get; set; }
}

public record Resolution(int Width, int Height);

public record ImageRecord
{
    public long Id { get; set; }
    public int CameraId { get; set; }
    public DateTime EventTime { get; set; }
    public required byte[] ImageData { get; set; }
}
