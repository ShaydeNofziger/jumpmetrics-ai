using System.Text;
using JumpMetrics.Core.Services;

namespace JumpMetrics.Core.Tests;

public class FlySightParserTests
{
    [Fact]
    public async Task ParseAsync_ValidSampleFile_ReturnsCorrectDataPoints()
    {
        // Arrange
        var samplePath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "..", "..", "..", "..", "..",
            "samples", "sample-jump.csv");
        
        var parser = new FlySightParser();

        // Act
        using var stream = File.OpenRead(samplePath);
        var dataPoints = await parser.ParseAsync(stream);

        // Assert
        Assert.NotNull(dataPoints);
        Assert.Equal(1972, dataPoints.Count); // Per CLAUDE.md spec
        
        // Verify first data point
        var first = dataPoints[0];
        Assert.Equal(new DateTime(2025, 9, 11, 17, 26, 18, 600, DateTimeKind.Utc), first.Time);
        Assert.Equal(34.7636471, first.Latitude, 7);
        Assert.Equal(-81.2017957, first.Longitude, 7);
        Assert.Equal(959.293, first.AltitudeMSL, 3);
        Assert.Equal(4, first.NumberOfSatellites);
        
        // Verify last data point
        var last = dataPoints[^1];
        Assert.Equal(new DateTime(2025, 9, 11, 17, 32, 52, 800, DateTimeKind.Utc), last.Time);
        
        // Verify metadata
        Assert.NotNull(parser.Metadata);
        Assert.Equal(1972, parser.Metadata.TotalDataPoints);
        Assert.Equal("v2023.09.22.2", parser.Metadata.FirmwareVersion);
        Assert.Equal("00190037484e501420353131", parser.Metadata.DeviceId);
        Assert.Equal("88e923ed802cfc8f2ade9528", parser.Metadata.SessionId);
        Assert.Equal(1, parser.Metadata.FlySightFormatVersion);
    }

    [Fact]
    public async Task ParseAsync_MinimalValidFile_ReturnsSingleDataPoint()
    {
        // Arrange
        var csv = @"$FLYS,1
$VAR,FIRMWARE_VER,v2023.09.22.2
$VAR,DEVICE_ID,test123
$VAR,SESSION_ID,session456
$COL,GNSS,time,lat,lon,hMSL,velN,velE,velD,hAcc,vAcc,sAcc,numSV
$UNIT,GNSS,,deg,deg,m,m/s,m/s,m/s,m,m,m/s,
$DATA
$GNSS,2025-09-11T17:26:18.600Z,34.7636471,-81.2017957,959.293,3.76,52.83,-8.82,293.141,1922.824,7.16,4";

        var parser = new FlySightParser();

        // Act
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var dataPoints = await parser.ParseAsync(stream);

        // Assert
        Assert.Single(dataPoints);
        Assert.NotNull(parser.Metadata);
        Assert.Equal(1, parser.Metadata.FlySightFormatVersion);
        Assert.Equal("v2023.09.22.2", parser.Metadata.FirmwareVersion);
    }

    [Fact]
    public async Task ParseAsync_EmptyStream_ReturnsEmptyList()
    {
        // Arrange
        var parser = new FlySightParser();

        // Act
        using var stream = new MemoryStream();
        var dataPoints = await parser.ParseAsync(stream);

        // Assert
        Assert.NotNull(dataPoints);
        Assert.Empty(dataPoints);
    }

    [Fact]
    public async Task ParseAsync_HeadersOnly_ReturnsEmptyList()
    {
        // Arrange
        var csv = @"$FLYS,1
$VAR,FIRMWARE_VER,v2023.09.22.2
$COL,GNSS,time,lat,lon,hMSL,velN,velE,velD,hAcc,vAcc,sAcc,numSV
$UNIT,GNSS,,deg,deg,m,m/s,m/s,m/s,m,m,m/s,
$DATA";

        var parser = new FlySightParser();

        // Act
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var dataPoints = await parser.ParseAsync(stream);

        // Assert
        Assert.Empty(dataPoints);
        Assert.NotNull(parser.Metadata);
        Assert.Equal("v2023.09.22.2", parser.Metadata.FirmwareVersion);
    }

    [Fact]
    public async Task ParseAsync_MalformedDataRow_SkipsRow()
    {
        // Arrange
        var csv = @"$FLYS,1
$COL,GNSS,time,lat,lon,hMSL,velN,velE,velD,hAcc,vAcc,sAcc,numSV
$UNIT,GNSS,,deg,deg,m,m/s,m/s,m/s,m,m,m/s,
$DATA
$GNSS,2025-09-11T17:26:18.600Z,34.7636471,-81.2017957,959.293,3.76,52.83,-8.82,293.141,1922.824,7.16,4
$GNSS,INVALID_TIME,34.7636471,-81.2017957,959.293,3.76,52.83,-8.82,293.141,1922.824,7.16,4
$GNSS,2025-09-11T17:26:19.000Z,34.7636318,-81.2015514,935.224,4.18,52.76,-7.52,123.437,782.141,2.90,4";

        var parser = new FlySightParser();

        // Act
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var dataPoints = await parser.ParseAsync(stream);

        // Assert
        Assert.Equal(2, dataPoints.Count); // Only 2 valid rows, malformed one skipped
    }

    [Fact]
    public async Task ParseAsync_DifferentColumnOrder_ParsesCorrectly()
    {
        // Arrange - Column order is different from typical
        var csv = @"$FLYS,1
$COL,GNSS,lat,lon,time,hMSL,velD,velN,velE,hAcc,vAcc,sAcc,numSV
$UNIT,GNSS,deg,deg,,m,m/s,m/s,m/s,m,m,m/s,
$DATA
$GNSS,34.7636471,-81.2017957,2025-09-11T17:26:18.600Z,959.293,-8.82,3.76,52.83,293.141,1922.824,7.16,4";

        var parser = new FlySightParser();

        // Act
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var dataPoints = await parser.ParseAsync(stream);

        // Assert
        Assert.Single(dataPoints);
        var dp = dataPoints[0];
        Assert.Equal(34.7636471, dp.Latitude, 7);
        Assert.Equal(-81.2017957, dp.Longitude, 7);
        Assert.Equal(959.293, dp.AltitudeMSL, 3);
        Assert.Equal(-8.82, dp.VelocityDown, 2);
        Assert.Equal(3.76, dp.VelocityNorth, 2);
        Assert.Equal(52.83, dp.VelocityEast, 2);
    }

    [Fact]
    public async Task ParseAsync_MissingFields_SkipsRow()
    {
        // Arrange - Missing required fields
        var csv = @"$FLYS,1
$COL,GNSS,time,lat,lon,hMSL,velN,velE,velD,hAcc,vAcc,sAcc,numSV
$UNIT,GNSS,,deg,deg,m,m/s,m/s,m/s,m,m,m/s,
$DATA
$GNSS,2025-09-11T17:26:18.600Z,34.7636471
$GNSS,2025-09-11T17:26:19.000Z,34.7636318,-81.2015514,935.224,4.18,52.76,-7.52,123.437,782.141,2.90,4";

        var parser = new FlySightParser();

        // Act
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var dataPoints = await parser.ParseAsync(stream);

        // Assert
        Assert.Single(dataPoints); // Only the complete row is parsed
    }

    [Fact]
    public async Task ParseAsync_MetadataExtraction_PopulatesAllFields()
    {
        // Arrange
        var csv = @"$FLYS,2
$VAR,FIRMWARE_VER,v2024.01.01.1
$VAR,DEVICE_ID,device789
$VAR,SESSION_ID,sess012
$COL,GNSS,time,lat,lon,hMSL,velN,velE,velD,hAcc,vAcc,sAcc,numSV
$UNIT,GNSS,,deg,deg,m,m/s,m/s,m/s,m,m,m/s,
$DATA
$GNSS,2025-09-11T17:26:18.600Z,34.7636471,-81.2017957,959.293,3.76,52.83,-8.82,293.141,1922.824,7.16,4
$GNSS,2025-09-11T17:26:19.600Z,34.7636017,-81.2012130,1907.554,5.64,53.15,-5.27,56.271,346.258,2.11,4";

        var parser = new FlySightParser();

        // Act
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var dataPoints = await parser.ParseAsync(stream);

        // Assert metadata
        Assert.NotNull(parser.Metadata);
        Assert.Equal(2, parser.Metadata.FlySightFormatVersion);
        Assert.Equal("v2024.01.01.1", parser.Metadata.FirmwareVersion);
        Assert.Equal("device789", parser.Metadata.DeviceId);
        Assert.Equal("sess012", parser.Metadata.SessionId);
        Assert.Equal(2, parser.Metadata.TotalDataPoints);
        Assert.Equal(new DateTime(2025, 9, 11, 17, 26, 18, 600, DateTimeKind.Utc), parser.Metadata.RecordingStart);
        Assert.Equal(new DateTime(2025, 9, 11, 17, 26, 19, 600, DateTimeKind.Utc), parser.Metadata.RecordingEnd);
        Assert.NotNull(parser.Metadata.MaxAltitude);
        Assert.Equal(1907.554, parser.Metadata.MaxAltitude.Value, 3);
        Assert.NotNull(parser.Metadata.MinAltitude);
        Assert.Equal(959.293, parser.Metadata.MinAltitude.Value, 3);
    }

    [Fact]
    public async Task ParseAsync_NonGNSSRows_SkipsSilently()
    {
        // Arrange - Include non-GNSS rows for forward compatibility
        var csv = @"$FLYS,1
$COL,GNSS,time,lat,lon,hMSL,velN,velE,velD,hAcc,vAcc,sAcc,numSV
$UNIT,GNSS,,deg,deg,m,m/s,m/s,m/s,m,m,m/s,
$DATA
$GNSS,2025-09-11T17:26:18.600Z,34.7636471,-81.2017957,959.293,3.76,52.83,-8.82,293.141,1922.824,7.16,4
$BARO,2025-09-11T17:26:18.600Z,1013.25,25.3
$GNSS,2025-09-11T17:26:19.000Z,34.7636318,-81.2015514,935.224,4.18,52.76,-7.52,123.437,782.141,2.90,4";

        var parser = new FlySightParser();

        // Act
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var dataPoints = await parser.ParseAsync(stream);

        // Assert
        Assert.Equal(2, dataPoints.Count); // Only GNSS rows parsed, BARO skipped
    }
}
