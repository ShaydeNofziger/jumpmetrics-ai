using JumpMetrics.Core.Models;

namespace JumpMetrics.Core.Interfaces;

/// <summary>
/// Orchestrates the complete jump processing pipeline locally
/// </summary>
public interface IJumpProcessor
{
    /// <summary>
    /// Processes a FlySight CSV file through the complete pipeline:
    /// parsing, validation, segmentation, and metrics calculation
    /// </summary>
    /// <param name="filePath">Path to the FlySight CSV file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Complete jump analysis with all metrics</returns>
    Task<Jump> ProcessJumpAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes a FlySight CSV stream through the complete pipeline
    /// </summary>
    /// <param name="stream">Stream containing FlySight CSV data</param>
    /// <param name="fileName">Name of the source file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Complete jump analysis with all metrics</returns>
    Task<Jump> ProcessJumpAsync(Stream stream, string fileName, CancellationToken cancellationToken = default);
}
