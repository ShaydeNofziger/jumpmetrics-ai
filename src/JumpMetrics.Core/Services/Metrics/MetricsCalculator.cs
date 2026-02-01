using JumpMetrics.Core.Interfaces;
using JumpMetrics.Core.Models;

namespace JumpMetrics.Core.Services.Metrics;

public class MetricsCalculator : IMetricsCalculator
{
    public JumpPerformanceMetrics Calculate(IReadOnlyList<JumpSegment> segments)
    {
        throw new NotImplementedException();
    }
}
