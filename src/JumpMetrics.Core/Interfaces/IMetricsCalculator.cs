using JumpMetrics.Core.Models;

namespace JumpMetrics.Core.Interfaces;

public interface IMetricsCalculator
{
    JumpPerformanceMetrics Calculate(IReadOnlyList<JumpSegment> segments);
}
