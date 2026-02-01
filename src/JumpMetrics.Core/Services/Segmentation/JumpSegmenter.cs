using JumpMetrics.Core.Interfaces;
using JumpMetrics.Core.Models;

namespace JumpMetrics.Core.Services.Segmentation;

public class JumpSegmenter : IJumpSegmenter
{
    public IReadOnlyList<JumpSegment> Segment(IReadOnlyList<DataPoint> dataPoints)
    {
        throw new NotImplementedException();
    }
}
