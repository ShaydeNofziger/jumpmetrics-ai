namespace JumpMetrics.Core.Models;

public class JumpMetadata
{
    public int TotalDataPoints { get; set; }
    public DateTime? RecordingStart { get; set; }
    public DateTime? RecordingEnd { get; set; }
    public double? MaxAltitude { get; set; }
    public double? MinAltitude { get; set; }
}
