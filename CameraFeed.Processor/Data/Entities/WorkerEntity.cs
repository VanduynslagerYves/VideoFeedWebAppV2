namespace CameraFeed.Processor.Data.Entities;

public class WorkerEntity
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
    public required ResolutionEntity Resolution { get; set; }
}

public record ResolutionEntity(string Id, int Width, int Height);

