using System.Globalization;
using JumpMetrics.Core.Interfaces;
using JumpMetrics.Core.Models;

namespace JumpMetrics.Core.Services;

public class FlySightParser : IFlySightParser
{
    public JumpMetadata? Metadata { get; private set; }

    public async Task<IReadOnlyList<DataPoint>> ParseAsync(Stream csvStream, CancellationToken cancellationToken = default)
    {
        var dataPoints = new List<DataPoint>();
        Metadata = new JumpMetadata();

        using var reader = new StreamReader(csvStream);
        
        // Parse header section
        Dictionary<string, int>? columnMapping = null;
        bool dataStarted = false;

        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            if (string.IsNullOrWhiteSpace(line))
                continue;

            // Check for $DATA sentinel
            if (line.Trim() == "$DATA")
            {
                dataStarted = true;
                continue;
            }

            if (!dataStarted)
            {
                // Parse header lines
                ParseHeaderLine(line);
                
                // Build column mapping from $COL line
                if (line.StartsWith("$COL,GNSS,"))
                {
                    columnMapping = BuildColumnMapping(line);
                }
            }
            else
            {
                // Parse data rows
                if (line.StartsWith("$GNSS,") && columnMapping != null)
                {
                    var dataPoint = ParseDataRow(line, columnMapping);
                    if (dataPoint != null)
                    {
                        dataPoints.Add(dataPoint);
                    }
                }
                // Silently skip non-GNSS rows for forward compatibility
            }
        }

        // Update metadata with aggregated values
        UpdateMetadata(dataPoints);

        return dataPoints;
    }

    private void ParseHeaderLine(string line)
    {
        var parts = line.Split(',');
        
        if (parts.Length == 0)
            return;

        switch (parts[0])
        {
            case "$FLYS":
                if (parts.Length >= 2 && int.TryParse(parts[1], out int version))
                {
                    Metadata!.FlySightFormatVersion = version;
                }
                break;
                
            case "$VAR":
                if (parts.Length >= 3)
                {
                    var key = parts[1];
                    var value = parts[2];
                    
                    switch (key)
                    {
                        case "FIRMWARE_VER":
                            Metadata!.FirmwareVersion = value;
                            break;
                        case "DEVICE_ID":
                            Metadata!.DeviceId = value;
                            break;
                        case "SESSION_ID":
                            Metadata!.SessionId = value;
                            break;
                    }
                }
                break;
        }
    }

    private Dictionary<string, int> BuildColumnMapping(string colLine)
    {
        var parts = colLine.Split(',');
        var mapping = new Dictionary<string, int>();
        
        // Skip "$COL" and "GNSS" parts, start from index 2
        for (int i = 2; i < parts.Length; i++)
        {
            var columnName = parts[i].Trim();
            if (!string.IsNullOrEmpty(columnName))
            {
                // Store the index in the data row (accounting for $GNSS prefix)
                mapping[columnName] = i - 1; // -1 because data rows start with $GNSS at index 0
            }
        }
        
        return mapping;
    }

    private DataPoint? ParseDataRow(string line, Dictionary<string, int> columnMapping)
    {
        try
        {
            var parts = line.Split(',');
            
            // Verify we have enough parts
            if (parts.Length < 2)
                return null;

            var dataPoint = new DataPoint();

            // Helper to safely get value at mapped index
            string? GetValue(string columnName)
            {
                if (columnMapping.TryGetValue(columnName, out int index) && index < parts.Length)
                {
                    return parts[index];
                }
                return null;
            }

            // Parse required fields
            var timeStr = GetValue("time");
            if (string.IsNullOrEmpty(timeStr) || !DateTimeOffset.TryParse(timeStr, out var timeOffset))
                return null;
            dataPoint.Time = timeOffset.UtcDateTime;

            var latStr = GetValue("lat");
            if (string.IsNullOrEmpty(latStr) || !double.TryParse(latStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat))
                return null;
            dataPoint.Latitude = lat;

            var lonStr = GetValue("lon");
            if (string.IsNullOrEmpty(lonStr) || !double.TryParse(lonStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var lon))
                return null;
            dataPoint.Longitude = lon;

            var altStr = GetValue("hMSL");
            if (string.IsNullOrEmpty(altStr) || !double.TryParse(altStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var alt))
                return null;
            dataPoint.AltitudeMSL = alt;

            // Parse velocity fields
            var velNStr = GetValue("velN");
            if (!string.IsNullOrEmpty(velNStr) && double.TryParse(velNStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var velN))
                dataPoint.VelocityNorth = velN;

            var velEStr = GetValue("velE");
            if (!string.IsNullOrEmpty(velEStr) && double.TryParse(velEStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var velE))
                dataPoint.VelocityEast = velE;

            var velDStr = GetValue("velD");
            if (!string.IsNullOrEmpty(velDStr) && double.TryParse(velDStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var velD))
                dataPoint.VelocityDown = velD;

            // Parse accuracy fields
            var hAccStr = GetValue("hAcc");
            if (!string.IsNullOrEmpty(hAccStr) && double.TryParse(hAccStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var hAcc))
                dataPoint.HorizontalAccuracy = hAcc;

            var vAccStr = GetValue("vAcc");
            if (!string.IsNullOrEmpty(vAccStr) && double.TryParse(vAccStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var vAcc))
                dataPoint.VerticalAccuracy = vAcc;

            var sAccStr = GetValue("sAcc");
            if (!string.IsNullOrEmpty(sAccStr) && double.TryParse(sAccStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var sAcc))
                dataPoint.SpeedAccuracy = sAcc;

            // Parse satellite count
            var numSVStr = GetValue("numSV");
            if (!string.IsNullOrEmpty(numSVStr) && int.TryParse(numSVStr, out var numSV))
                dataPoint.NumberOfSatellites = numSV;

            return dataPoint;
        }
        catch
        {
            // Skip malformed rows
            return null;
        }
    }

    private void UpdateMetadata(List<DataPoint> dataPoints)
    {
        if (Metadata == null || dataPoints.Count == 0)
            return;

        Metadata.TotalDataPoints = dataPoints.Count;
        Metadata.RecordingStart = dataPoints.Min(dp => dp.Time);
        Metadata.RecordingEnd = dataPoints.Max(dp => dp.Time);
        Metadata.MaxAltitude = dataPoints.Max(dp => dp.AltitudeMSL);
        Metadata.MinAltitude = dataPoints.Min(dp => dp.AltitudeMSL);
    }
}
