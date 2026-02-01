using JumpMetrics.Core.Services.Validation;

namespace JumpMetrics.Core.Tests;

public class DataValidatorTests
{
    [Fact]
    public void Validate_EmptyDataPoints_ThrowsNotImplemented()
    {
        var validator = new DataValidator();

        // TODO: Implement test once validator is built
        Assert.Throws<NotImplementedException>(
            () => validator.Validate([]));
    }
}
