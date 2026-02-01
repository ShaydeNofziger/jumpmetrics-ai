using JumpMetrics.Core.Models;

namespace JumpMetrics.Core.Interfaces;

public interface IStorageService
{
    /// <summary>
    /// Uploads a CSV file to blob storage
    /// </summary>
    Task<string> UploadFlySightFileAsync(string fileName, Stream fileStream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores jump metrics in table storage
    /// </summary>
    Task StoreJumpMetricsAsync(Jump jump, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves jump metrics from table storage
    /// </summary>
    Task<Jump?> GetJumpMetricsAsync(Guid jumpId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all jumps from table storage
    /// </summary>
    Task<IReadOnlyList<Jump>> ListJumpsAsync(CancellationToken cancellationToken = default);
}
