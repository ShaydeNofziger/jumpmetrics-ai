namespace JumpMetrics.Core.Models;

public class JumpMetadata
{
    public int TotalDataPoints { get; set; }
    public DateTime? RecordingStart { get; set; }
    public DateTime? RecordingEnd { get; set; }
    public double? MaxAltitude { get; set; }
    public double? MinAltitude { get; set; }

    // FlySight v2 header metadata (populated from $VAR lines)
    public string? FirmwareVersion { get; set; }     // $VAR,FIRMWARE_VER
    public string? DeviceId { get; set; }            // $VAR,DEVICE_ID
    public string? SessionId { get; set; }           // $VAR,SESSION_ID
    public int? FlySightFormatVersion { get; set; }  // $FLYS version number
}
