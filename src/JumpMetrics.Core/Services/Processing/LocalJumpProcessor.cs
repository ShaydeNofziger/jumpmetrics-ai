using JumpMetrics.Core.Interfaces;
using JumpMetrics.Core.Models;
using Microsoft.Extensions.Logging;

namespace JumpMetrics.Core.Services.Processing;

/// <summary>
/// Orchestrates the complete jump processing pipeline locally
/// </summary>
public class LocalJumpProcessor : IJumpProcessor
{
    private readonly IFlySightParser _parser;
    private readonly IDataValidator _validator;
    private readonly IJumpSegmenter _segmenter;
    private readonly IMetricsCalculator _metricsCalculator;
    private readonly ILogger<LocalJumpProcessor>? _logger;

    public LocalJumpProcessor(
        IFlySightParser parser,
        IDataValidator validator,
        IJumpSegmenter segmenter,
        IMetricsCalculator metricsCalculator,
        ILogger<LocalJumpProcessor>? logger = null)
    {
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _segmenter = segmenter ?? throw new ArgumentNullException(nameof(segmenter));
        _metricsCalculator = metricsCalculator ?? throw new ArgumentNullException(nameof(metricsCalculator));
        _logger = logger;
    }

    public async Task<Jump> ProcessJumpAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"FlySight file not found: {filePath}");

        _logger?.LogInformation("Processing FlySight file: {FilePath}", filePath);

        using var stream = File.OpenRead(filePath);
        var fileName = Path.GetFileName(filePath);

        return await ProcessJumpAsync(stream, fileName, cancellationToken);
    }

    public async Task<Jump> ProcessJumpAsync(Stream stream, string fileName, CancellationToken cancellationToken = default)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be null or empty", nameof(fileName));

        _logger?.LogInformation("Processing FlySight data from stream: {FileName}", fileName);

        // Step 1: Parse the FlySight CSV file
        IReadOnlyList<DataPoint> dataPoints;
        try
        {
            dataPoints = await _parser.ParseAsync(stream, cancellationToken);
            _logger?.LogInformation("Parsed {Count} data points from {FileName}", dataPoints.Count, fileName);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error parsing FlySight file {FileName}", fileName);
            throw new InvalidOperationException($"Failed to parse FlySight CSV file: {ex.Message}", ex);
        }

        // Step 2: Validate the data
        var validationResult = _validator.Validate(dataPoints);
        if (!validationResult.IsValid)
        {
            _logger?.LogWarning("Data validation failed for {FileName}: {Errors}", fileName, string.Join(", ", validationResult.Errors));
            throw new InvalidOperationException($"Data validation failed: {string.Join(", ", validationResult.Errors)}");
        }

        if (validationResult.Warnings.Count > 0)
        {
            _logger?.LogWarning("Data validation warnings for {FileName}: {Warnings}", fileName, string.Join(", ", validationResult.Warnings));
        }

        // Step 3: Segment the jump
        IReadOnlyList<JumpSegment> segments;
        try
        {
            segments = _segmenter.Segment(dataPoints);
            _logger?.LogInformation("Segmented jump into {Count} segments", segments.Count);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error segmenting jump from {FileName}", fileName);
            throw new InvalidOperationException($"Failed to segment jump: {ex.Message}", ex);
        }

        // Step 4: Calculate metrics
        JumpPerformanceMetrics? metrics = null;
        try
        {
            metrics = _metricsCalculator.Calculate(segments);
            _logger?.LogInformation("Calculated performance metrics");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error calculating metrics from {FileName}", fileName);
            _logger?.LogWarning("Continuing without metrics due to calculation error");
        }

        // Step 5: Create Jump object
        var jump = new Jump
        {
            JumpId = Guid.NewGuid(),
            JumpDate = DateTime.UtcNow,
            FlySightFileName = fileName,
            Segments = segments.ToList(),
            Metrics = metrics
        };

        // Populate metadata if available
        if (dataPoints.Count > 0)
        {
            jump.Metadata.TotalDataPoints = dataPoints.Count;
            jump.Metadata.RecordingStart = dataPoints.First().Time;
            jump.Metadata.RecordingEnd = dataPoints.Last().Time;
            jump.Metadata.MaxAltitude = dataPoints.Max(dp => dp.AltitudeMSL);
            jump.Metadata.MinAltitude = dataPoints.Min(dp => dp.AltitudeMSL);
        }

        // Copy FlySight metadata from parser if available
        if (_parser.Metadata != null)
        {
            jump.Metadata.FirmwareVersion = _parser.Metadata.FirmwareVersion;
            jump.Metadata.DeviceId = _parser.Metadata.DeviceId;
            jump.Metadata.SessionId = _parser.Metadata.SessionId;
            jump.Metadata.FlySightFormatVersion = _parser.Metadata.FlySightFormatVersion;
        }

        _logger?.LogInformation("Successfully processed jump {JumpId}", jump.JumpId);

        return jump;
    }
}
