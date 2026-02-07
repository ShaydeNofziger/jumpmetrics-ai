using JumpMetrics.Core.Models;

namespace JumpMetrics.Core.Interfaces;

public interface IFlySightParser
{
    JumpMetadata? Metadata { get; }
    Task<IReadOnlyList<DataPoint>> ParseAsync(Stream csvStream, CancellationToken cancellationToken = default);
}
