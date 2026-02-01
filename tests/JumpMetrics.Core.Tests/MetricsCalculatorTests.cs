using JumpMetrics.Core.Services.Metrics;

namespace JumpMetrics.Core.Tests;

public class MetricsCalculatorTests
{
    [Fact]
    public void Calculate_EmptySegments_ThrowsNotImplemented()
    {
        var calculator = new MetricsCalculator();

        // TODO: Implement test once calculator is built
        Assert.Throws<NotImplementedException>(
            () => calculator.Calculate([]));
    }
}
