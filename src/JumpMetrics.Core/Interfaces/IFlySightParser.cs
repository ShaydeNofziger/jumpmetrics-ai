using JumpMetrics.Core.Models;

namespace JumpMetrics.Core.Interfaces;

public interface IFlySightParser
{
    Task<IReadOnlyList<DataPoint>> ParseAsync(Stream csvStream, CancellationToken cancellationToken = default);
}
