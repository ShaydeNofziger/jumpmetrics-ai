using JumpMetrics.Core.Models;

namespace JumpMetrics.Core.Interfaces;

public interface IDataValidator
{
    ValidationResult Validate(IReadOnlyList<DataPoint> dataPoints);
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}
