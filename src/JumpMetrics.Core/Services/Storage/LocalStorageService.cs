using System.Text.Json;
using System.Text.Json.Serialization;
using JumpMetrics.Core.Interfaces;
using JumpMetrics.Core.Models;
using Microsoft.Extensions.Logging;

namespace JumpMetrics.Core.Services.Storage;

/// <summary>
/// Provides local file-based storage for jump data using JSON files
/// </summary>
public class LocalStorageService : ILocalStorageService
{
    private readonly string _storageDirectory;
    private readonly ILogger<LocalStorageService>? _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public LocalStorageService(string storageDirectory, ILogger<LocalStorageService>? logger = null)
    {
        if (string.IsNullOrWhiteSpace(storageDirectory))
            throw new ArgumentException("Storage directory cannot be null or empty", nameof(storageDirectory));

        _storageDirectory = storageDirectory;
        _logger = logger;

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // Ensure storage directory exists
        if (!Directory.Exists(_storageDirectory))
        {
            Directory.CreateDirectory(_storageDirectory);
            _logger?.LogInformation("Created storage directory: {Directory}", _storageDirectory);
        }
    }

    public async Task SaveJumpAsync(Jump jump, CancellationToken cancellationToken = default)
    {
        if (jump == null)
            throw new ArgumentNullException(nameof(jump));

        var filePath = GetJumpFilePath(jump.JumpId);
        _logger?.LogInformation("Saving jump {JumpId} to {FilePath}", jump.JumpId, filePath);

        try
        {
            var json = JsonSerializer.Serialize(jump, _jsonOptions);
            await File.WriteAllTextAsync(filePath, json, cancellationToken);
            _logger?.LogInformation("Successfully saved jump {JumpId}", jump.JumpId);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error saving jump {JumpId}", jump.JumpId);
            throw;
        }
    }

    public async Task<Jump?> GetJumpAsync(Guid jumpId, CancellationToken cancellationToken = default)
    {
        var filePath = GetJumpFilePath(jumpId);

        if (!File.Exists(filePath))
        {
            _logger?.LogWarning("Jump file not found for {JumpId}", jumpId);
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath, cancellationToken);
            var jump = JsonSerializer.Deserialize<Jump>(json, _jsonOptions);
            _logger?.LogInformation("Successfully loaded jump {JumpId}", jumpId);
            return jump;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error loading jump {JumpId}", jumpId);
            throw;
        }
    }

    public async Task<IReadOnlyList<Jump>> ListJumpsAsync(CancellationToken cancellationToken = default)
    {
        var jumps = new List<Jump>();

        if (!Directory.Exists(_storageDirectory))
        {
            _logger?.LogInformation("Storage directory does not exist, returning empty list");
            return jumps;
        }

        var files = Directory.GetFiles(_storageDirectory, "*.json");
        _logger?.LogInformation("Found {Count} jump files", files.Length);

        foreach (var file in files)
        {
            try
            {
                var json = await File.ReadAllTextAsync(file, cancellationToken);
                var jump = JsonSerializer.Deserialize<Jump>(json, _jsonOptions);
                if (jump != null)
                {
                    jumps.Add(jump);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error loading jump from {File}", file);
            }
        }

        // Sort by jump date descending (most recent first)
        jumps.Sort((a, b) => b.JumpDate.CompareTo(a.JumpDate));

        return jumps;
    }

    public async Task DeleteJumpAsync(Guid jumpId, CancellationToken cancellationToken = default)
    {
        var filePath = GetJumpFilePath(jumpId);

        if (!File.Exists(filePath))
        {
            _logger?.LogWarning("Jump file not found for deletion: {JumpId}", jumpId);
            return;
        }

        try
        {
            File.Delete(filePath);
            _logger?.LogInformation("Successfully deleted jump {JumpId}", jumpId);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error deleting jump {JumpId}", jumpId);
            throw;
        }

        await Task.CompletedTask;
    }

    private string GetJumpFilePath(Guid jumpId)
    {
        return Path.Combine(_storageDirectory, $"{jumpId}.json");
    }
}
