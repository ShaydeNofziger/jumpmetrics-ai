using JumpMetrics.Core.Interfaces;
using JumpMetrics.Core.Models;

namespace JumpMetrics.Core.Services.Validation;

public class DataValidator : IDataValidator
{
    public ValidationResult Validate(IReadOnlyList<DataPoint> dataPoints)
    {
        throw new NotImplementedException();
    }
}
