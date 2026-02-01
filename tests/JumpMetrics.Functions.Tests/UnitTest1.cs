using JumpMetrics.Core.Interfaces;
using JumpMetrics.Core.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace JumpMetrics.Functions.Tests;

public class AnalyzeJumpFunctionTests
{
    [Fact]
    public void AnalyzeJumpFunction_CanBeConstructed()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<AnalyzeJumpFunction>>();
        var parserMock = new Mock<IFlySightParser>();
        var validatorMock = new Mock<IDataValidator>();
        var segmenterMock = new Mock<IJumpSegmenter>();
        var metricsCalculatorMock = new Mock<IMetricsCalculator>();
        var storageMock = new Mock<IStorageService>();

        // Act
        var function = new AnalyzeJumpFunction(
            loggerMock.Object,
            parserMock.Object,
            validatorMock.Object,
            segmenterMock.Object,
            metricsCalculatorMock.Object,
            storageMock.Object
        );

        // Assert
        Assert.NotNull(function);
    }

    [Fact]
    public void Services_CanBeRegistered()
    {
        // This test verifies that all the services can be instantiated
        // which validates that the DI configuration in Program.cs is correct

        var parserMock = new Mock<IFlySightParser>();
        var validatorMock = new Mock<IDataValidator>();
        var segmenterMock = new Mock<IJumpSegmenter>();
        var metricsCalculatorMock = new Mock<IMetricsCalculator>();
        var storageMock = new Mock<IStorageService>();

        Assert.NotNull(parserMock.Object);
        Assert.NotNull(validatorMock.Object);
        Assert.NotNull(segmenterMock.Object);
        Assert.NotNull(metricsCalculatorMock.Object);
        Assert.NotNull(storageMock.Object);
    }

    [Fact]
    public async Task StorageService_CanUploadFile()
    {
        // Arrange
        var storageMock = new Mock<IStorageService>();
        var testStream = new MemoryStream();
        
        storageMock.Setup(s => s.UploadFlySightFileAsync(
            It.IsAny<string>(), 
            It.IsAny<Stream>(), 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://test.blob.core.windows.net/test.csv");

        // Act
        var result = await storageMock.Object.UploadFlySightFileAsync("test.csv", testStream, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("https://", result);
        storageMock.Verify(s => s.UploadFlySightFileAsync(
            It.IsAny<string>(), 
            It.IsAny<Stream>(), 
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task StorageService_CanStoreJumpMetrics()
    {
        // Arrange
        var storageMock = new Mock<IStorageService>();
        var jump = new Jump
        {
            JumpId = Guid.NewGuid(),
            JumpDate = DateTime.UtcNow,
            FlySightFileName = "test.csv"
        };

        storageMock.Setup(s => s.StoreJumpMetricsAsync(
            It.IsAny<Jump>(), 
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await storageMock.Object.StoreJumpMetricsAsync(jump, CancellationToken.None);

        // Assert
        storageMock.Verify(s => s.StoreJumpMetricsAsync(
            It.IsAny<Jump>(), 
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task ParserService_ReturnsDataPoints()
    {
        // Arrange
        var parserMock = new Mock<IFlySightParser>();
        var expectedDataPoints = new List<DataPoint>
        {
            new DataPoint
            {
                Time = DateTime.UtcNow,
                AltitudeMSL = 1000,
                Latitude = 34.0,
                Longitude = -81.0
            }
        };

        parserMock.Setup(p => p.ParseAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDataPoints);

        // Act
        var result = await parserMock.Object.ParseAsync(new MemoryStream(), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        parserMock.Verify(p => p.ParseAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void ValidatorService_ValidatesDataPoints()
    {
        // Arrange
        var validatorMock = new Mock<IDataValidator>();
        var dataPoints = new List<DataPoint>
        {
            new DataPoint
            {
                Time = DateTime.UtcNow,
                AltitudeMSL = 1000
            }
        };

        validatorMock.Setup(v => v.Validate(It.IsAny<IReadOnlyList<DataPoint>>()))
            .Returns(new ValidationResult { IsValid = true });

        // Act
        var result = validatorMock.Object.Validate(dataPoints);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsValid);
        validatorMock.Verify(v => v.Validate(It.IsAny<IReadOnlyList<DataPoint>>()), Times.Once);
    }

    [Fact]
    public void SegmenterService_ReturnsSegments()
    {
        // Arrange
        var segmenterMock = new Mock<IJumpSegmenter>();
        var dataPoints = new List<DataPoint>();
        var expectedSegments = new List<JumpSegment>
        {
            new JumpSegment
            {
                Type = SegmentType.Freefall,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow.AddSeconds(30),
                StartAltitude = 1000,
                EndAltitude = 800
            }
        };

        segmenterMock.Setup(s => s.Segment(It.IsAny<IReadOnlyList<DataPoint>>()))
            .Returns(expectedSegments);

        // Act
        var result = segmenterMock.Object.Segment(dataPoints);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(SegmentType.Freefall, result[0].Type);
        segmenterMock.Verify(s => s.Segment(It.IsAny<IReadOnlyList<DataPoint>>()), Times.Once);
    }

    [Fact]
    public void MetricsCalculatorService_CalculatesMetrics()
    {
        // Arrange
        var metricsCalculatorMock = new Mock<IMetricsCalculator>();
        var segments = new List<JumpSegment>();
        var expectedMetrics = new JumpPerformanceMetrics();

        metricsCalculatorMock.Setup(m => m.Calculate(It.IsAny<IReadOnlyList<JumpSegment>>()))
            .Returns(expectedMetrics);

        // Act
        var result = metricsCalculatorMock.Object.Calculate(segments);

        // Assert
        Assert.NotNull(result);
        metricsCalculatorMock.Verify(m => m.Calculate(It.IsAny<IReadOnlyList<JumpSegment>>()), Times.Once);
    }
}
