using JumpMetrics.Core.Models;
using JumpMetrics.Core.Services.Storage;
using Microsoft.Extensions.Logging;
using Moq;

namespace JumpMetrics.Core.Tests;

public class LocalStorageServiceTests : IDisposable
{
    private readonly string _testStorageDirectory;
    private readonly LocalStorageService _service;

    public LocalStorageServiceTests()
    {
        _testStorageDirectory = Path.Combine(Path.GetTempPath(), $"JumpMetricsTests_{Guid.NewGuid()}");
        var mockLogger = new Mock<ILogger<LocalStorageService>>();
        _service = new LocalStorageService(_testStorageDirectory, mockLogger.Object);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testStorageDirectory))
        {
            Directory.Delete(_testStorageDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task SaveJumpAsync_WithValidJump_SavesFileSuccessfully()
    {
        // Arrange
        var jump = CreateTestJump();

        // Act
        await _service.SaveJumpAsync(jump);

        // Assert
        var filePath = Path.Combine(_testStorageDirectory, $"{jump.JumpId}.json");
        Assert.True(File.Exists(filePath));
    }

    [Fact]
    public async Task GetJumpAsync_WithExistingJump_ReturnsJump()
    {
        // Arrange
        var jump = CreateTestJump();
        await _service.SaveJumpAsync(jump);

        // Act
        var retrievedJump = await _service.GetJumpAsync(jump.JumpId);

        // Assert
        Assert.NotNull(retrievedJump);
        Assert.Equal(jump.JumpId, retrievedJump.JumpId);
        Assert.Equal(jump.FlySightFileName, retrievedJump.FlySightFileName);
        Assert.Equal(jump.Metadata.TotalDataPoints, retrievedJump.Metadata.TotalDataPoints);
    }

    [Fact]
    public async Task GetJumpAsync_WithNonExistentJump_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _service.GetJumpAsync(nonExistentId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SaveAndRetrieveJump_RoundTripsCorrectly()
    {
        // Arrange
        var jump = CreateCompleteTestJump();

        // Act
        await _service.SaveJumpAsync(jump);
        var retrievedJump = await _service.GetJumpAsync(jump.JumpId);

        // Assert
        Assert.NotNull(retrievedJump);
        Assert.Equal(jump.JumpId, retrievedJump.JumpId);
        Assert.Equal(jump.JumpDate, retrievedJump.JumpDate);
        Assert.Equal(jump.FlySightFileName, retrievedJump.FlySightFileName);
        Assert.Equal(jump.Segments.Count, retrievedJump.Segments.Count);
        Assert.Equal(jump.Metadata.MaxAltitude, retrievedJump.Metadata.MaxAltitude);
        Assert.Equal(jump.Metadata.MinAltitude, retrievedJump.Metadata.MinAltitude);
    }

    [Fact]
    public async Task ListJumpsAsync_WithNoJumps_ReturnsEmptyList()
    {
        // Act
        var jumps = await _service.ListJumpsAsync();

        // Assert
        Assert.NotNull(jumps);
        Assert.Empty(jumps);
    }

    [Fact]
    public async Task ListJumpsAsync_WithMultipleJumps_ReturnsAllJumps()
    {
        // Arrange
        var jump1 = CreateTestJump();
        jump1.JumpDate = DateTime.UtcNow.AddDays(-2);
        var jump2 = CreateTestJump();
        jump2.JumpDate = DateTime.UtcNow.AddDays(-1);
        var jump3 = CreateTestJump();
        jump3.JumpDate = DateTime.UtcNow;

        await _service.SaveJumpAsync(jump1);
        await _service.SaveJumpAsync(jump2);
        await _service.SaveJumpAsync(jump3);

        // Act
        var jumps = await _service.ListJumpsAsync();

        // Assert
        Assert.Equal(3, jumps.Count);
        // Should be sorted by date descending (most recent first)
        Assert.Equal(jump3.JumpId, jumps[0].JumpId);
        Assert.Equal(jump2.JumpId, jumps[1].JumpId);
        Assert.Equal(jump1.JumpId, jumps[2].JumpId);
    }

    [Fact]
    public async Task ListJumpsAsync_WithCorruptFile_SkipsCorruptFile()
    {
        // Arrange
        var validJump = CreateTestJump();
        await _service.SaveJumpAsync(validJump);

        // Create a corrupt JSON file
        var corruptFilePath = Path.Combine(_testStorageDirectory, $"{Guid.NewGuid()}.json");
        await File.WriteAllTextAsync(corruptFilePath, "{ invalid json }");

        // Act
        var jumps = await _service.ListJumpsAsync();

        // Assert - Should only return the valid jump
        Assert.Single(jumps);
        Assert.Equal(validJump.JumpId, jumps[0].JumpId);
    }

    [Fact]
    public async Task DeleteJumpAsync_WithExistingJump_DeletesFile()
    {
        // Arrange
        var jump = CreateTestJump();
        await _service.SaveJumpAsync(jump);
        var filePath = Path.Combine(_testStorageDirectory, $"{jump.JumpId}.json");
        Assert.True(File.Exists(filePath));

        // Act
        await _service.DeleteJumpAsync(jump.JumpId);

        // Assert
        Assert.False(File.Exists(filePath));
    }

    [Fact]
    public async Task DeleteJumpAsync_WithNonExistentJump_DoesNotThrow()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert - Should not throw
        await _service.DeleteJumpAsync(nonExistentId);
    }

    [Fact]
    public async Task SaveJumpAsync_WithNullJump_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _service.SaveJumpAsync(null!));
    }

    [Fact]
    public void Constructor_WithNullStorageDirectory_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new LocalStorageService(null!, null));
    }

    [Fact]
    public void Constructor_WithEmptyStorageDirectory_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new LocalStorageService(string.Empty, null));
    }

    [Fact]
    public void Constructor_CreatesDirectoryIfNotExists()
    {
        // Arrange
        var newDirectory = Path.Combine(Path.GetTempPath(), $"JumpMetricsNew_{Guid.NewGuid()}");
        Assert.False(Directory.Exists(newDirectory));

        try
        {
            // Act
            var service = new LocalStorageService(newDirectory, null);

            // Assert
            Assert.True(Directory.Exists(newDirectory));
        }
        finally
        {
            if (Directory.Exists(newDirectory))
            {
                Directory.Delete(newDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ListJumpsAsync_WhenDirectoryDoesNotExist_ReturnsEmptyList()
    {
        // Arrange - Delete the directory after construction
        var tempDir = Path.Combine(Path.GetTempPath(), $"JumpMetricsTemp_{Guid.NewGuid()}");
        var service = new LocalStorageService(tempDir, null);
        Directory.Delete(tempDir);

        // Act
        var jumps = await service.ListJumpsAsync();

        // Assert
        Assert.NotNull(jumps);
        Assert.Empty(jumps);
    }

    [Fact]
    public async Task SaveJumpAsync_WithComplexJump_PreservesAllData()
    {
        // Arrange
        var jump = CreateCompleteTestJump();
        jump.Metrics = new JumpPerformanceMetrics
        {
            Freefall = new FreefallMetrics
            {
                TimeInFreefall = 45.5,
                AverageVerticalSpeed = 50.2,
                MaxVerticalSpeed = 65.8,
                AverageHorizontalSpeed = 25.3,
                TrackAngle = 180.5
            },
            Canopy = new CanopyMetrics
            {
                DeploymentAltitude = 1500,
                AverageDescentRate = 5.2,
                GlideRatio = 2.5,
                MaxHorizontalSpeed = 30.1,
                TotalCanopyTime = 120.0,
                PatternAltitude = 500
            },
            Landing = new LandingMetrics
            {
                FinalApproachSpeed = 8.5,
                TouchdownVerticalSpeed = 2.1,
                LandingAccuracy = 10.5
            }
        };

        // Act
        await _service.SaveJumpAsync(jump);
        var retrievedJump = await _service.GetJumpAsync(jump.JumpId);

        // Assert
        Assert.NotNull(retrievedJump);
        Assert.NotNull(retrievedJump.Metrics);
        Assert.NotNull(retrievedJump.Metrics.Freefall);
        Assert.Equal(45.5, retrievedJump.Metrics.Freefall.TimeInFreefall);
        Assert.Equal(50.2, retrievedJump.Metrics.Freefall.AverageVerticalSpeed);
        Assert.NotNull(retrievedJump.Metrics.Canopy);
        Assert.Equal(1500, retrievedJump.Metrics.Canopy.DeploymentAltitude);
        Assert.NotNull(retrievedJump.Metrics.Landing);
        Assert.Equal(8.5, retrievedJump.Metrics.Landing.FinalApproachSpeed);
    }

    private Jump CreateTestJump()
    {
        return new Jump
        {
            JumpId = Guid.NewGuid(),
            JumpDate = DateTime.UtcNow,
            FlySightFileName = "test-jump.csv",
            Metadata = new JumpMetadata
            {
                TotalDataPoints = 1000,
                RecordingStart = DateTime.UtcNow.AddMinutes(-10),
                RecordingEnd = DateTime.UtcNow,
                MaxAltitude = 3000,
                MinAltitude = 200,
                FirmwareVersion = "v2023.09.22",
                DeviceId = "TEST123"
            },
            Segments = new List<JumpSegment>(),
            Metrics = null
        };
    }

    private Jump CreateCompleteTestJump()
    {
        var jump = CreateTestJump();
        jump.Segments = new List<JumpSegment>
        {
            new JumpSegment
            {
                Type = SegmentType.Aircraft,
                StartTime = DateTime.UtcNow.AddMinutes(-10),
                EndTime = DateTime.UtcNow.AddMinutes(-8),
                StartAltitude = 500,
                EndAltitude = 3000,
                DataPoints = new List<DataPoint>()
            },
            new JumpSegment
            {
                Type = SegmentType.Freefall,
                StartTime = DateTime.UtcNow.AddMinutes(-8),
                EndTime = DateTime.UtcNow.AddMinutes(-7),
                StartAltitude = 3000,
                EndAltitude = 1500,
                DataPoints = new List<DataPoint>()
            },
            new JumpSegment
            {
                Type = SegmentType.Canopy,
                StartTime = DateTime.UtcNow.AddMinutes(-7),
                EndTime = DateTime.UtcNow.AddMinutes(-2),
                StartAltitude = 1500,
                EndAltitude = 200,
                DataPoints = new List<DataPoint>()
            }
        };
        return jump;
    }
}
