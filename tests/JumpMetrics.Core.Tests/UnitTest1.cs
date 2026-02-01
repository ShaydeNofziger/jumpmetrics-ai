using JumpMetrics.Core.Services;

namespace JumpMetrics.Core.Tests;

public class FlySightParserTests
{
    [Fact]
    public async Task ParseAsync_EmptyStream_ReturnsEmptyList()
    {
        var parser = new FlySightParser();
        using var stream = new MemoryStream();

        // TODO: Implement test once parser is built
        await Assert.ThrowsAsync<NotImplementedException>(
            () => parser.ParseAsync(stream));
    }
}
