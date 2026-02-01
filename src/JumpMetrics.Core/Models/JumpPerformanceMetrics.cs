namespace JumpMetrics.Core.Models;

public class JumpPerformanceMetrics
{
    public FreefallMetrics? Freefall { get; set; }
    public CanopyMetrics? Canopy { get; set; }
    public LandingMetrics? Landing { get; set; }
}

public class FreefallMetrics
{
    public double AverageVerticalSpeed { get; set; }
    public double MaxVerticalSpeed { get; set; }
    public double AverageHorizontalSpeed { get; set; }
    public double TrackAngle { get; set; }
    public double TimeInFreefall { get; set; }
}

public class CanopyMetrics
{
    public double DeploymentAltitude { get; set; }
    public double AverageDescentRate { get; set; }
    public double GlideRatio { get; set; }
    public double MaxHorizontalSpeed { get; set; }
    public double TotalCanopyTime { get; set; }
    public double? PatternAltitude { get; set; }
}

public class LandingMetrics
{
    public double FinalApproachSpeed { get; set; }
    public double TouchdownVerticalSpeed { get; set; }
    public double? LandingAccuracy { get; set; }
}
