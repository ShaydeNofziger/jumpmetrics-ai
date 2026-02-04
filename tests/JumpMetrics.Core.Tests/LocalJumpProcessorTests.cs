using JumpMetrics.Core.Interfaces;
using JumpMetrics.Core.Models;
using JumpMetrics.Core.Services.Processing;
using Microsoft.Extensions.Logging;
using Moq;

namespace JumpMetrics.Core.Tests;

public class LocalJumpProcessorTests
{
    private readonly Mock<IFlySightParser> _mockParser;
    private readonly Mock<IDataValidator> _mockValidator;
    private readonly Mock<IJumpSegmenter> _mockSegmenter;
    private readonly Mock<IMetricsCalculator> _mockMetricsCalculator;
    private readonly Mock<ILogger<LocalJumpProcessor>> _mockLogger;
    private readonly LocalJumpProcessor _processor;

    public LocalJumpProcessorTests()
    {
        _mockParser = new Mock<IFlySightParser>();
        _mockValidator = new Mock<IDataValidator>();
        _mockSegmenter = new Mock<IJumpSegmenter>();
        _mockMetricsCalculator = new Mock<IMetricsCalculator>();
        _mockLogger = new Mock<ILogger<LocalJumpProcessor>>();

        _processor = new LocalJumpProcessor(
            _mockParser.Object,
            _mockValidator.Object,
            _mockSegmenter.Object,
            _mockMetricsCalculator.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task ProcessJumpAsync_WithValidFilePath_ReturnsCompleteJump()
    {
        // Arrange
        var testFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(testFile, "test data");

        var dataPoints = new List<DataPoint>
        {
            new DataPoint
            {
                Time = DateTime.UtcNow,
                AltitudeMSL = 1000,
                VelocityDown = 10,
                VelocityNorth = 5,
                VelocityEast = 3
            }
        };

        var segments = new List<JumpSegment>
        {
            new JumpSegment
            {
                Type = SegmentType.Freefall,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow.AddSeconds(10),
                StartAltitude = 1000,
                EndAltitude = 900
            }
        };

        var metrics = new JumpPerformanceMetrics();

        _mockParser.Setup(p => p.ParseAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dataPoints);
        _mockValidator.Setup(v => v.Validate(It.IsAny<IReadOnlyList<DataPoint>>()))
            .Returns(new ValidationResult { IsValid = true });
        _mockSegmenter.Setup(s => s.Segment(It.IsAny<IReadOnlyList<DataPoint>>()))
            .Returns(segments);
        _mockMetricsCalculator.Setup(m => m.Calculate(It.IsAny<IReadOnlyList<JumpSegment>>()))
            .Returns(metrics);

        try
        {
            // Act
            var result = await _processor.ProcessJumpAsync(testFile);

            // Assert
            Assert.NotNull(result);
            Assert.NotEqual(Guid.Empty, result.JumpId);
            Assert.Equal(Path.GetFileName(testFile), result.FlySightFileName);
            Assert.Single(result.Segments);
            Assert.Equal(metrics, result.Metrics);
            Assert.Equal(dataPoints.Count, result.Metadata.TotalDataPoints);
        }
        finally
        {
            File.Delete(testFile);
        }
    }

    [Fact]
    public async Task ProcessJumpAsync_WithStream_ReturnsCompleteJump()
    {
        // Arrange
        var dataPoints = new List<DataPoint>
        {
            new DataPoint
            {
                Time = DateTime.UtcNow,
                AltitudeMSL = 2000,
                VelocityDown = 20
            }
        };

        var segments = new List<JumpSegment>
        {
            new JumpSegment { Type = SegmentType.Canopy }
        };

        var metrics = new JumpPerformanceMetrics();

        _mockParser.Setup(p => p.ParseAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dataPoints);
        _mockValidator.Setup(v => v.Validate(It.IsAny<IReadOnlyList<DataPoint>>()))
            .Returns(new ValidationResult { IsValid = true });
        _mockSegmenter.Setup(s => s.Segment(It.IsAny<IReadOnlyList<DataPoint>>()))
            .Returns(segments);
        _mockMetricsCalculator.Setup(m => m.Calculate(It.IsAny<IReadOnlyList<JumpSegment>>()))
            .Returns(metrics);

        using var stream = new MemoryStream();
        await stream.WriteAsync(System.Text.Encoding.UTF8.GetBytes("test"));
        stream.Position = 0;

        // Act
        var result = await _processor.ProcessJumpAsync(stream, "test.csv");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test.csv", result.FlySightFileName);
        Assert.Single(result.Segments);
    }

    [Fact]
    public async Task ProcessJumpAsync_WithInvalidFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentFile = "/tmp/does-not-exist.csv";

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(
            async () => await _processor.ProcessJumpAsync(nonExistentFile));
    }

    [Fact]
    public async Task ProcessJumpAsync_WithNullFilePath_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _processor.ProcessJumpAsync((string)null!));
    }

    [Fact]
    public async Task ProcessJumpAsync_WithEmptyFilePath_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _processor.ProcessJumpAsync(string.Empty));
    }

    [Fact]
    public async Task ProcessJumpAsync_WhenParserFails_ThrowsInvalidOperationException()
    {
        // Arrange
        var testFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(testFile, "test data");

        _mockParser.Setup(p => p.ParseAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Parse error"));

        try
        {
            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _processor.ProcessJumpAsync(testFile));
            Assert.Contains("Failed to parse FlySight CSV file", ex.Message);
        }
        finally
        {
            File.Delete(testFile);
        }
    }

    [Fact]
    public async Task ProcessJumpAsync_WhenValidationFails_ThrowsInvalidOperationException()
    {
        // Arrange
        var testFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(testFile, "test data");

        var dataPoints = new List<DataPoint>
        {
            new DataPoint { Time = DateTime.UtcNow }
        };

        _mockParser.Setup(p => p.ParseAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dataPoints);
        _mockValidator.Setup(v => v.Validate(It.IsAny<IReadOnlyList<DataPoint>>()))
            .Returns(new ValidationResult
            {
                IsValid = false,
                Errors = new List<string> { "Insufficient data points" }
            });

        try
        {
            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _processor.ProcessJumpAsync(testFile));
            Assert.Contains("Data validation failed", ex.Message);
        }
        finally
        {
            File.Delete(testFile);
        }
    }

    [Fact]
    public async Task ProcessJumpAsync_WhenSegmentationFails_ThrowsInvalidOperationException()
    {
        // Arrange
        var testFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(testFile, "test data");

        var dataPoints = new List<DataPoint>
        {
            new DataPoint { Time = DateTime.UtcNow }
        };

        _mockParser.Setup(p => p.ParseAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dataPoints);
        _mockValidator.Setup(v => v.Validate(It.IsAny<IReadOnlyList<DataPoint>>()))
            .Returns(new ValidationResult { IsValid = true });
        _mockSegmenter.Setup(s => s.Segment(It.IsAny<IReadOnlyList<DataPoint>>()))
            .Throws(new InvalidOperationException("Segmentation error"));

        try
        {
            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _processor.ProcessJumpAsync(testFile));
            Assert.Contains("Failed to segment jump", ex.Message);
        }
        finally
        {
            File.Delete(testFile);
        }
    }

    [Fact]
    public async Task ProcessJumpAsync_WhenMetricsCalculationFails_ContinuesWithoutMetrics()
    {
        // Arrange
        var testFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(testFile, "test data");

        var dataPoints = new List<DataPoint>
        {
            new DataPoint
            {
                Time = DateTime.UtcNow,
                AltitudeMSL = 1000
            }
        };

        var segments = new List<JumpSegment>
        {
            new JumpSegment { Type = SegmentType.Freefall }
        };

        _mockParser.Setup(p => p.ParseAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dataPoints);
        _mockValidator.Setup(v => v.Validate(It.IsAny<IReadOnlyList<DataPoint>>()))
            .Returns(new ValidationResult { IsValid = true });
        _mockSegmenter.Setup(s => s.Segment(It.IsAny<IReadOnlyList<DataPoint>>()))
            .Returns(segments);
        _mockMetricsCalculator.Setup(m => m.Calculate(It.IsAny<IReadOnlyList<JumpSegment>>()))
            .Throws(new Exception("Metrics calculation error"));

        try
        {
            // Act
            var result = await _processor.ProcessJumpAsync(testFile);

            // Assert - Should continue without metrics
            Assert.NotNull(result);
            Assert.Null(result.Metrics);
            Assert.Single(result.Segments);
        }
        finally
        {
            File.Delete(testFile);
        }
    }

    [Fact]
    public async Task ProcessJumpAsync_PopulatesMetadataCorrectly()
    {
        // Arrange
        var testFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(testFile, "test data");

        var startTime = DateTime.UtcNow;
        var endTime = startTime.AddMinutes(5);

        var dataPoints = new List<DataPoint>
        {
            new DataPoint { Time = startTime, AltitudeMSL = 500 },
            new DataPoint { Time = startTime.AddMinutes(1), AltitudeMSL = 2000 },
            new DataPoint { Time = endTime, AltitudeMSL = 300 }
        };

        var segments = new List<JumpSegment>();
        var metrics = new JumpPerformanceMetrics();

        _mockParser.Setup(p => p.ParseAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dataPoints);
        _mockValidator.Setup(v => v.Validate(It.IsAny<IReadOnlyList<DataPoint>>()))
            .Returns(new ValidationResult { IsValid = true });
        _mockSegmenter.Setup(s => s.Segment(It.IsAny<IReadOnlyList<DataPoint>>()))
            .Returns(segments);
        _mockMetricsCalculator.Setup(m => m.Calculate(It.IsAny<IReadOnlyList<JumpSegment>>()))
            .Returns(metrics);

        try
        {
            // Act
            var result = await _processor.ProcessJumpAsync(testFile);

            // Assert
            Assert.Equal(3, result.Metadata.TotalDataPoints);
            Assert.Equal(startTime, result.Metadata.RecordingStart);
            Assert.Equal(endTime, result.Metadata.RecordingEnd);
            Assert.Equal(2000, result.Metadata.MaxAltitude);
            Assert.Equal(300, result.Metadata.MinAltitude);
        }
        finally
        {
            File.Delete(testFile);
        }
    }
}
