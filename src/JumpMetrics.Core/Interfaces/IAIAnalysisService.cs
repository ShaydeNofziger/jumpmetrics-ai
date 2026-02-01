using JumpMetrics.Core.Models;

namespace JumpMetrics.Core.Interfaces;

public interface IAIAnalysisService
{
    Task<AIAnalysis> AnalyzeAsync(Jump jump, CancellationToken cancellationToken = default);
}
