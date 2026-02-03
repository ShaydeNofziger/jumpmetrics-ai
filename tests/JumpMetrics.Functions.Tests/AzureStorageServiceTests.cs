using Azure.Data.Tables;
using Azure.Storage.Blobs;
using JumpMetrics.Core.Models;
using JumpMetrics.Functions.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;

namespace JumpMetrics.Functions.Tests;

public class AzureStorageServiceTests
{
    [Fact]
    public void SegmentSummaries_ShouldExcludeDataPoints_ToAvoidSizeLimit()
    {
        // Arrange - Create a jump with segments containing many data points
        var jump = CreateJumpWithLargeSegments();

        // Create segment summaries as done in StoreJumpMetricsAsync
        var segmentSummaries = jump.Segments.Select(s => new
        {
            Type = s.Type,
            StartTime = s.StartTime,
            EndTime = s.EndTime,
            StartAltitude = s.StartAltitude,
            EndAltitude = s.EndAltitude,
            Duration = s.Duration,
            DataPointCount = s.DataPoints.Count
        }).ToList();

        // Act - Serialize the summaries
        var json = JsonSerializer.Serialize(segmentSummaries);

        // Assert - Verify the JSON is much smaller than serializing full segments
        var fullJson = JsonSerializer.Serialize(jump.Segments);
        
        Assert.True(json.Length < fullJson.Length, 
            $"Segment summaries ({json.Length} chars) should be smaller than full segments ({fullJson.Length} chars)");
        
        // Verify it's well under the 32K character limit for Azure Table Storage string properties
        Assert.True(json.Length < 32000, 
            $"Serialized segment summaries ({json.Length} chars) should be under 32K character limit");
        
        // Verify the summary still contains essential information
        Assert.Contains("\"Type\":", json);
        Assert.Contains("\"StartTime\":", json);
        Assert.Contains("\"EndTime\":", json);
        Assert.Contains("\"DataPointCount\":", json);
        
        // Verify DataPoints are NOT in the summary
        Assert.DoesNotContain("\"DataPoints\":", json);
        Assert.DoesNotContain("\"Latitude\":", json);
        Assert.DoesNotContain("\"Longitude\":", json);
    }

    [Fact]
    public void SegmentSummary_Deserialization_ShouldRecreateSegmentsWithoutDataPoints()
    {
        // Arrange - Create segment summaries
        var originalSegment = new JumpSegment
        {
            Type = SegmentType.Freefall,
            StartTime = DateTime.Parse("2026-02-02T12:00:00Z"),
            EndTime = DateTime.Parse("2026-02-02T12:00:30Z"),
            StartAltitude = 3000,
            EndAltitude = 2000,
            DataPoints = CreateSampleDataPoints(500) // Many data points
        };

        var segmentSummary = new
        {
            Type = originalSegment.Type,
            StartTime = originalSegment.StartTime,
            EndTime = originalSegment.EndTime,
            StartAltitude = originalSegment.StartAltitude,
            EndAltitude = originalSegment.EndAltitude,
            Duration = originalSegment.Duration,
            DataPointCount = originalSegment.DataPoints.Count
        };

        // Act - Serialize and deserialize as done in MapEntityToJump
        var json = JsonSerializer.Serialize(new[] { segmentSummary });
        var segmentSummaries = JsonSerializer.Deserialize<List<JsonElement>>(json);
        
        Assert.NotNull(segmentSummaries);
        var deserialized = segmentSummaries.Select(s => new JumpSegment
        {
            Type = ParseSegmentType(s.GetProperty("Type")),
            StartTime = s.GetProperty("StartTime").GetDateTime(),
            EndTime = s.GetProperty("EndTime").GetDateTime(),
            StartAltitude = s.GetProperty("StartAltitude").GetDouble(),
            EndAltitude = s.GetProperty("EndAltitude").GetDouble(),
            DataPoints = []
        }).ToList();

        // Assert - Verify segment properties are preserved
        Assert.Single(deserialized);
        var segment = deserialized[0];
        
        Assert.Equal(SegmentType.Freefall, segment.Type);
        Assert.Equal(originalSegment.StartTime, segment.StartTime);
        Assert.Equal(originalSegment.EndTime, segment.EndTime);
        Assert.Equal(originalSegment.StartAltitude, segment.StartAltitude);
        Assert.Equal(originalSegment.EndAltitude, segment.EndAltitude);
        
        // DataPoints should be empty
        Assert.Empty(segment.DataPoints);
    }

    [Fact]
    public void RealWorldScenario_With1972DataPoints_ShouldSerializeUnder64KB()
    {
        // Arrange - Simulate the real scenario from the bug report with 1972 data points
        var jump = CreateJumpWithRealWorldDataSize(1972);

        // Act - Create segment summaries
        var segmentSummaries = jump.Segments.Select(s => new
        {
            Type = s.Type,
            StartTime = s.StartTime,
            EndTime = s.EndTime,
            StartAltitude = s.StartAltitude,
            EndAltitude = s.EndAltitude,
            Duration = s.Duration,
            DataPointCount = s.DataPoints.Count
        }).ToList();

        var segmentsJson = JsonSerializer.Serialize(segmentSummaries);
        var metricsJson = JsonSerializer.Serialize(jump.Metrics);
        var metadataJson = JsonSerializer.Serialize(jump.Metadata);

        // Assert - Each property should be well under 64KB (65,536 bytes) limit
        var segmentsSizeInBytes = System.Text.Encoding.UTF8.GetByteCount(segmentsJson);
        var metricsSizeInBytes = System.Text.Encoding.UTF8.GetByteCount(metricsJson);
        var metadataSizeInBytes = System.Text.Encoding.UTF8.GetByteCount(metadataJson);

        Assert.True(segmentsSizeInBytes < 65536, 
            $"SegmentsJson size ({segmentsSizeInBytes} bytes) exceeds 64KB limit");
        Assert.True(metricsSizeInBytes < 65536, 
            $"MetricsJson size ({metricsSizeInBytes} bytes) exceeds 64KB limit");
        Assert.True(metadataSizeInBytes < 65536, 
            $"MetadataJson size ({metadataSizeInBytes} bytes) exceeds 64KB limit");

        // Verify the summaries are actually small
        Assert.True(segmentsSizeInBytes < 5000, 
            $"Segment summaries should be very small (actual: {segmentsSizeInBytes} bytes)");
    }

    private static Jump CreateJumpWithLargeSegments()
    {
        return new Jump
        {
            JumpId = Guid.NewGuid(),
            JumpDate = DateTime.UtcNow,
            FlySightFileName = "test-jump.csv",
            Segments = new List<JumpSegment>
            {
                new JumpSegment
                {
                    Type = SegmentType.Freefall,
                    StartTime = DateTime.UtcNow,
                    EndTime = DateTime.UtcNow.AddSeconds(30),
                    StartAltitude = 3000,
                    EndAltitude = 2000,
                    DataPoints = CreateSampleDataPoints(150)
                },
                new JumpSegment
                {
                    Type = SegmentType.Canopy,
                    StartTime = DateTime.UtcNow.AddSeconds(30),
                    EndTime = DateTime.UtcNow.AddSeconds(180),
                    StartAltitude = 2000,
                    EndAltitude = 300,
                    DataPoints = CreateSampleDataPoints(750)
                }
            },
            Metrics = new JumpPerformanceMetrics(),
            Metadata = new JumpMetadata
            {
                TotalDataPoints = 900
            }
        };
    }

    private static Jump CreateJumpWithRealWorldDataSize(int totalDataPoints)
    {
        // Distribute data points across segments similar to real jumps
        var freefallPoints = (int)(totalDataPoints * 0.075); // ~7.5% in freefall (15 sec at 5Hz)
        var canopyPoints = (int)(totalDataPoints * 0.8);      // ~80% under canopy
        var landingPoints = totalDataPoints - freefallPoints - canopyPoints;

        return new Jump
        {
            JumpId = Guid.NewGuid(),
            JumpDate = DateTime.UtcNow,
            FlySightFileName = "sample-jump.csv",
            Segments = new List<JumpSegment>
            {
                new JumpSegment
                {
                    Type = SegmentType.Aircraft,
                    StartTime = DateTime.UtcNow,
                    EndTime = DateTime.UtcNow.AddSeconds(120),
                    StartAltitude = 960,
                    EndAltitude = 1910,
                    DataPoints = CreateSampleDataPoints((int)(totalDataPoints * 0.125))
                },
                new JumpSegment
                {
                    Type = SegmentType.Freefall,
                    StartTime = DateTime.UtcNow.AddSeconds(120),
                    EndTime = DateTime.UtcNow.AddSeconds(135),
                    StartAltitude = 1910,
                    EndAltitude = 1780,
                    DataPoints = CreateSampleDataPoints(freefallPoints)
                },
                new JumpSegment
                {
                    Type = SegmentType.Canopy,
                    StartTime = DateTime.UtcNow.AddSeconds(135),
                    EndTime = DateTime.UtcNow.AddSeconds(390),
                    StartAltitude = 1780,
                    EndAltitude = 300,
                    DataPoints = CreateSampleDataPoints(canopyPoints)
                },
                new JumpSegment
                {
                    Type = SegmentType.Landing,
                    StartTime = DateTime.UtcNow.AddSeconds(390),
                    EndTime = DateTime.UtcNow.AddSeconds(395),
                    StartAltitude = 300,
                    EndAltitude = 193,
                    DataPoints = CreateSampleDataPoints(landingPoints)
                }
            },
            Metrics = new JumpPerformanceMetrics
            {
                Freefall = new FreefallMetrics
                {
                    AverageVerticalSpeed = 25.5,
                    MaxVerticalSpeed = 28.3,
                    TimeInFreefall = 15.2
                },
                Canopy = new CanopyMetrics
                {
                    DeploymentAltitude = 1780,
                    AverageDescentRate = 4.5,
                    TotalCanopyTime = 255.0
                }
            },
            Metadata = new JumpMetadata
            {
                TotalDataPoints = totalDataPoints,
                MaxAltitude = 1910,
                MinAltitude = 193
            }
        };
    }

    private static List<DataPoint> CreateSampleDataPoints(int count)
    {
        var dataPoints = new List<DataPoint>();
        var baseTime = DateTime.UtcNow;

        for (int i = 0; i < count; i++)
        {
            dataPoints.Add(new DataPoint
            {
                Time = baseTime.AddMilliseconds(i * 200.0), // 5Hz = 200ms
                Latitude = 34.0 + (i * 0.0001),
                Longitude = -81.0 + (i * 0.0001),
                AltitudeMSL = 3000 - (i * 5),
                VelocityNorth = 10.0,
                VelocityEast = 5.0,
                VelocityDown = 20.0,
                HorizontalAccuracy = 5.0,
                VerticalAccuracy = 10.0,
                SpeedAccuracy = 2.0,
                NumberOfSatellites = 12
            });
        }

        return dataPoints;
    }

    private static SegmentType ParseSegmentType(JsonElement typeElement)
    {
        // Use System.Text.Json's enum deserialization rather than re-implementing
        // the production parsing logic, to avoid test/production drift.
        try
        {
            var raw = typeElement.GetRawText();
            var parsed = JsonSerializer.Deserialize<SegmentType>(raw);
            return parsed;
        }
        catch (JsonException)
        {
            // Fall back to the default enum value if deserialization fails.
            return default;
        }
    }
}
