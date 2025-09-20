using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CameraFeed.SocketServer.Data;

public class ApiKeyDbContext(DbContextOptions<ApiKeyDbContext> options) : DbContext(options)
{
    public DbSet<ApiKeyEntity> ApiKeys { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApiKeyEntity>(MapApiKeyEntity);
    }

    protected static void MapApiKeyEntity(EntityTypeBuilder<ApiKeyEntity> builder)
    {
        builder.ToTable("keys").HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.KeyHash).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.UserId).IsRequired();
    }
}

public class ApiKeyEntity
{
    public int Id { get; set; }
    public required string KeyHash { get; set; } = null!;
    public required DateTime CreatedAt { get; set; }
    public required bool IsActive { get; set; }
    public required int UserId { get; set; }
}