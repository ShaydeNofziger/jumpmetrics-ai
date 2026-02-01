using JumpMetrics.Core.Models;
using JumpMetrics.Core.Services.Validation;

namespace JumpMetrics.Core.Tests;

public class DataValidatorTests
{
    [Fact]
    public void Validate_ValidDataset_PassesWithNoErrorsOrWarnings()
    {
        // Arrange
        var validator = new DataValidator();
        var dataPoints = CreateValidDataPoints(20);

        // Act
        var result = validator.Validate(dataPoints);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Validate_EmptyList_ReturnsError()
    {
        // Arrange
        var validator = new DataValidator();

        // Act
        var result = validator.Validate([]);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("No data points provided", result.Errors);
    }

    [Fact]
    public void Validate_NullList_ReturnsError()
    {
        // Arrange
        var validator = new DataValidator();

        // Act
        var result = validator.Validate(null!);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("No data points provided", result.Errors);
    }

    [Fact]
    public void Validate_TooFewDataPoints_ReturnsError()
    {
        // Arrange
        var validator = new DataValidator();
        var dataPoints = CreateValidDataPoints(5); // Less than minimum of 10

        // Act
        var result = validator.Validate(dataPoints);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Insufficient data points", result.Errors[0]);
    }

    [Fact]
    public void Validate_AllTimestampsIdentical_ReturnsError()
    {
        // Arrange
        var validator = new DataValidator();
        var baseTime = DateTime.UtcNow;
        var dataPoints = Enumerable.Range(0, 15)
            .Select(i => new DataPoint
            {
                Time = baseTime, // All same time
                Latitude = 34.76,
                Longitude = -81.20,
                AltitudeMSL = 1000 + i,
                VelocityDown = 10,
                HorizontalAccuracy = 10,
                NumberOfSatellites = 8
            })
            .ToList();

        // Act
        var result = validator.Validate(dataPoints);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("All timestamps are identical", result.Errors[0]);
    }

    [Fact]
    public void Validate_PoorGpsAccuracy_ReturnsWarning()
    {
        // Arrange
        var validator = new DataValidator();
        var dataPoints = CreateValidDataPoints(15);
        
        // Make first 5 points have poor accuracy (> 50m)
        for (int i = 0; i < 5; i++)
        {
            dataPoints[i].HorizontalAccuracy = 100.0;
        }

        // Act
        var result = validator.Validate(dataPoints);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Single(result.Warnings);
        Assert.Contains("poor GPS accuracy", result.Warnings[0]);
        Assert.Contains("5 data points", result.Warnings[0]);
    }

    [Fact]
    public void Validate_InsufficientSatellites_ReturnsWarning()
    {
        // Arrange
        var validator = new DataValidator();
        var dataPoints = CreateValidDataPoints(15);
        
        // Make first 3 points have insufficient satellites (< 6)
        for (int i = 0; i < 3; i++)
        {
            dataPoints[i].NumberOfSatellites = 4;
        }

        // Act
        var result = validator.Validate(dataPoints);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Single(result.Warnings);
        Assert.Contains("insufficient satellites", result.Warnings[0]);
        Assert.Contains("3 data points", result.Warnings[0]);
    }

    [Fact]
    public void Validate_NonMonotonicTimestamps_ReturnsWarning()
    {
        // Arrange
        var validator = new DataValidator();
        var dataPoints = CreateValidDataPoints(15);
        
        // Swap two timestamps to make them non-monotonic
        var temp = dataPoints[5].Time;
        dataPoints[5].Time = dataPoints[6].Time;
        dataPoints[6].Time = temp;

        // Act
        var result = validator.Validate(dataPoints);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Contains("not monotonically increasing", result.Warnings[0]);
    }

    [Fact]
    public void Validate_LargeTimeGap_ReturnsWarning()
    {
        // Arrange
        var validator = new DataValidator();
        var dataPoints = CreateValidDataPoints(15);
        
        // Create a large gap (> 2 seconds) between points 7 and 8
        dataPoints[8].Time = dataPoints[7].Time.AddSeconds(5.0);
        
        // Update subsequent times to maintain monotonic order
        for (int i = 9; i < dataPoints.Count; i++)
        {
            dataPoints[i].Time = dataPoints[8].Time.AddMilliseconds(200 * (i - 8));
        }

        // Act
        var result = validator.Validate(dataPoints);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Contains("Large time gap", result.Warnings[0]);
        Assert.Contains("5.0s", result.Warnings[0]);
    }

    [Fact]
    public void Validate_AltitudeOutOfRange_ReturnsWarning()
    {
        // Arrange
        var validator = new DataValidator();
        var dataPoints = CreateValidDataPoints(15);
        
        // Set some altitudes outside reasonable range
        dataPoints[0].AltitudeMSL = -200.0; // Below -100m
        dataPoints[1].AltitudeMSL = 15000.0; // Above 10000m

        // Act
        var result = validator.Validate(dataPoints);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Contains("altitude outside reasonable range", result.Warnings[0]);
        Assert.Contains("2 data points", result.Warnings[0]);
    }

    [Fact]
    public void Validate_ImplausibleVelocity_ReturnsWarning()
    {
        // Arrange
        var validator = new DataValidator();
        var dataPoints = CreateValidDataPoints(15);
        
        // Set implausible velocities (> 150 m/s)
        dataPoints[0].VelocityDown = 200.0;
        dataPoints[1].VelocityDown = -180.0;

        // Act
        var result = validator.Validate(dataPoints);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Contains("implausible velocity", result.Warnings[0]);
        Assert.Contains("2 data points", result.Warnings[0]);
    }

    [Fact]
    public void Validate_MultipleWarnings_ReturnsAllWarnings()
    {
        // Arrange
        var validator = new DataValidator();
        var dataPoints = CreateValidDataPoints(20);
        
        // Add multiple issues
        dataPoints[0].HorizontalAccuracy = 100.0; // Poor accuracy
        dataPoints[1].NumberOfSatellites = 3; // Low satellites
        dataPoints[2].VelocityDown = 200.0; // Implausible velocity
        dataPoints[3].AltitudeMSL = -200.0; // Out of range altitude

        // Act
        var result = validator.Validate(dataPoints);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Equal(4, result.Warnings.Count);
    }

    [Fact]
    public void Validate_GpsAcquisitionNoise_ReturnsExpectedWarnings()
    {
        // Arrange
        var validator = new DataValidator();
        var dataPoints = CreateValidDataPoints(20);
        
        // Simulate GPS acquisition phase (first 5 points)
        for (int i = 0; i < 5; i++)
        {
            dataPoints[i].HorizontalAccuracy = 150.0;
            dataPoints[i].NumberOfSatellites = 4;
        }

        // Act
        var result = validator.Validate(dataPoints);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Contains(result.Warnings, w => w.Contains("poor GPS accuracy"));
        Assert.Contains(result.Warnings, w => w.Contains("insufficient satellites"));
    }

    // Helper method to create valid data points
    private List<DataPoint> CreateValidDataPoints(int count)
    {
        var baseTime = new DateTime(2025, 9, 11, 17, 26, 18, DateTimeKind.Utc);
        var dataPoints = new List<DataPoint>();
        
        for (int i = 0; i < count; i++)
        {
            dataPoints.Add(new DataPoint
            {
                Time = baseTime.AddMilliseconds(200 * i), // 5 Hz = 200ms intervals
                Latitude = 34.76 + (i * 0.0001),
                Longitude = -81.20 - (i * 0.0001),
                AltitudeMSL = 1000 - (i * 5), // Descending
                VelocityNorth = 5.0,
                VelocityEast = 10.0,
                VelocityDown = 15.0,
                HorizontalAccuracy = 10.0,
                VerticalAccuracy = 15.0,
                SpeedAccuracy = 1.0,
                NumberOfSatellites = 10
            });
        }
        
        return dataPoints;
    }
}

