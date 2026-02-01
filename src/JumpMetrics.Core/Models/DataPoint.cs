namespace JumpMetrics.Core.Models;

/// <summary>
/// A single GPS data point from a FlySight 2 CSV record.
/// </summary>
public class DataPoint
{
    public DateTime Time { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double AltitudeMSL { get; set; }
    public double VelocityNorth { get; set; }
    public double VelocityEast { get; set; }
    public double VelocityDown { get; set; }
    public double HorizontalAccuracy { get; set; }
    public double VerticalAccuracy { get; set; }
    public double SpeedAccuracy { get; set; }
    public int NumberOfSatellites { get; set; }

    public double HorizontalSpeed => Math.Sqrt(VelocityNorth * VelocityNorth + VelocityEast * VelocityEast);
    public double VerticalSpeed => Math.Abs(VelocityDown);
    public double GroundTrack => Math.Atan2(VelocityEast, VelocityNorth);
}
