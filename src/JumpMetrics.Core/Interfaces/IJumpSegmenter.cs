using JumpMetrics.Core.Models;

namespace JumpMetrics.Core.Interfaces;

public interface IJumpSegmenter
{
    IReadOnlyList<JumpSegment> Segment(IReadOnlyList<DataPoint> dataPoints);
}
