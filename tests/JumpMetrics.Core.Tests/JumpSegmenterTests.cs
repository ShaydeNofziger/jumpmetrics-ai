using JumpMetrics.Core.Services.Segmentation;

namespace JumpMetrics.Core.Tests;

public class JumpSegmenterTests
{
    [Fact]
    public void Segment_EmptyDataPoints_ThrowsNotImplemented()
    {
        var segmenter = new JumpSegmenter();

        // TODO: Implement test once segmenter is built
        Assert.Throws<NotImplementedException>(
            () => segmenter.Segment([]));
    }
}
