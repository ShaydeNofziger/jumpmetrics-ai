namespace JumpMetrics.Core.Models;

public class Jump
{
    public Guid JumpId { get; set; }
    public DateTime JumpDate { get; set; }
    public required string FlySightFileName { get; set; }
    public string? BlobUri { get; set; }

    public JumpMetadata Metadata { get; set; } = new();
    public List<JumpSegment> Segments { get; set; } = [];
    public JumpPerformanceMetrics? Metrics { get; set; }
    public AIAnalysis? Analysis { get; set; }
}
