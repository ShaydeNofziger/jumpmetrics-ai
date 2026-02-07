using JumpMetrics.Core.Models;

namespace JumpMetrics.Core.Interfaces;

/// <summary>
/// Provides local file-based storage for jump data
/// </summary>
public interface ILocalStorageService
{
    /// <summary>
    /// Saves jump data to local storage
    /// </summary>
    Task SaveJumpAsync(Jump jump, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves jump data from local storage
    /// </summary>
    Task<Jump?> GetJumpAsync(Guid jumpId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all jumps from local storage
    /// </summary>
    Task<IReadOnlyList<Jump>> ListJumpsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a jump from local storage
    /// </summary>
    Task DeleteJumpAsync(Guid jumpId, CancellationToken cancellationToken = default);
}
