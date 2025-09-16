using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CameraFeed.Processor.Data;

public class CamDbContext(DbContextOptions<CamDbContext> options) : DbContext(options)
{
    public DbSet<WorkerRecord> WorkerRecords { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkerRecord>(MapWorkerRecord);
        modelBuilder.Entity<ResolutionRecord>(MapResolutionRecord);
    }

    protected static void MapWorkerRecord(EntityTypeBuilder<WorkerRecord> recordBuilder)
    {
        recordBuilder.ToTable("worker").HasKey(x => x.Id);
        recordBuilder.Property(x => x.Id).ValueGeneratedOnAdd();

        recordBuilder.Property(x => x.Name).IsRequired().HasMaxLength(50);
        recordBuilder.Property(x => x.CameraId).IsRequired();
        recordBuilder.Property(x => x.Enabled).IsRequired();
        recordBuilder.Property(x => x.Framerate).IsRequired();
        recordBuilder.Property(x => x.UseMotiondetection).IsRequired();
        recordBuilder.Property(x => x.DownscaleRatio).IsRequired();
        recordBuilder.Property(x => x.MotionRatio).IsRequired();
        
        recordBuilder.HasOne(x => x.Resolution).WithMany().HasForeignKey("ResolutionId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        recordBuilder.HasIndex(x => x.CameraId).IsUnique();
    }

    protected static void MapResolutionRecord(EntityTypeBuilder<ResolutionRecord> recordBuilder)
    {
        recordBuilder.ToTable("resolution").HasKey(x => x.Id);
        recordBuilder.Property(x => x.Id).ValueGeneratedOnAdd();

        recordBuilder.Property(x => x.Width).IsRequired();
        recordBuilder.Property(x => x.Height).IsRequired();

        recordBuilder.HasIndex(x => new { x.Width, x.Height }).IsUnique();
    }
}

public class WorkerRecord
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required int CameraId { get; set; }
    public required bool Enabled { get; set; }
    public required int Framerate { get; set; }
    public required bool UseMotiondetection { get; set; }
    public required int DownscaleRatio { get; set; }
    public required double MotionRatio { get; set; }

    public required string ResolutionId { get; set; }
    public required ResolutionRecord Resolution { get; set; }
}

public record ResolutionRecord(string Id, int Width, int Height);
