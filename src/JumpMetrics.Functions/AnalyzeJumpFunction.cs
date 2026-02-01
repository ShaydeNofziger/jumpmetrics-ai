using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace JumpMetrics.Functions;

public class AnalyzeJumpFunction(ILogger<AnalyzeJumpFunction> logger)
{
    [Function("AnalyzeJump")]
    public Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "jumps/analyze")] HttpRequestData req,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("AnalyzeJump function triggered.");
        throw new NotImplementedException();
    }
}
