using JumpMetrics.Core.Interfaces;
using JumpMetrics.Core.Models;

namespace JumpMetrics.Core.Services;

public class FlySightParser : IFlySightParser
{
    public Task<IReadOnlyList<DataPoint>> ParseAsync(Stream csvStream, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
