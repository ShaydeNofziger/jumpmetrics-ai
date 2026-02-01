namespace JumpMetrics.Core.Models;

public class JumpSegment
{
    public SegmentType Type { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public double StartAltitude { get; set; }
    public double EndAltitude { get; set; }
    public double Duration => (EndTime - StartTime).TotalSeconds;
    public List<DataPoint> DataPoints { get; set; } = [];
}
