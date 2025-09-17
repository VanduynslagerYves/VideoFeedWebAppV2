using CameraFeed.Processor.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CameraFeed.Processor.Data;

public class CamDbContext(DbContextOptions<CamDbContext> options) : DbContext(options)
{
    public DbSet<WorkerEntity> WorkerRecords { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkerEntity>(MapWorkerRecord);
        modelBuilder.Entity<ResolutionEntity>(MapResolutionRecord);
    }

    protected static void MapWorkerRecord(EntityTypeBuilder<WorkerEntity> recordBuilder)
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

    protected static void MapResolutionRecord(EntityTypeBuilder<ResolutionEntity> recordBuilder)
    {
        recordBuilder.ToTable("resolution").HasKey(x => x.Id);
        recordBuilder.Property(x => x.Id).ValueGeneratedOnAdd();

        recordBuilder.Property(x => x.Width).IsRequired();
        recordBuilder.Property(x => x.Height).IsRequired();

        recordBuilder.HasIndex(x => new { x.Width, x.Height }).IsUnique();
    }
}