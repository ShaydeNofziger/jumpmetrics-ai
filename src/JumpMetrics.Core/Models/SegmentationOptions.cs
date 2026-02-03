namespace JumpMetrics.Core.Models;

/// <summary>
/// Configuration options for jump phase segmentation.
/// </summary>
public class SegmentationOptions
{
    /// <summary>
    /// Minimum smoothed velD to consider as freefall (m/s).
    /// Default covers hop-n-pops which may only reach 15-25 m/s.
    /// </summary>
    public double MinFreefallVelD { get; set; } = 10.0;

    /// <summary>
    /// Rate of velD decrease that indicates deployment (m/s²).
    /// Sustained deceleration over 2+ seconds.
    /// Reduced from 5.0 to 1.0 to detect hop-n-pop deployments (10-15 m/s decel over 10 samples).
    /// </summary>
    public double DeploymentDecelThreshold { get; set; } = 1.0;

    /// <summary>
    /// Minimum canopy descent rate (m/s).
    /// </summary>
    public double MinCanopyVelD { get; set; } = 2.0;

    /// <summary>
    /// Maximum canopy descent rate (m/s).
    /// </summary>
    public double MaxCanopyVelD { get; set; } = 15.0;

    /// <summary>
    /// Maximum velD to consider "on the ground" (m/s).
    /// </summary>
    public double LandingVelDThreshold { get; set; } = 1.0;

    /// <summary>
    /// Maximum horizontal speed to consider "stopped" (m/s).
    /// </summary>
    public double LandingHorizontalThreshold { get; set; } = 2.0;

    /// <summary>
    /// Maximum hAcc for data points to be used in segmentation (meters).
    /// </summary>
    public double GpsAccuracyThreshold { get; set; } = 50.0;

    /// <summary>
    /// Number of samples for sliding window average (smoothing).
    /// </summary>
    public int SmoothingWindowSize { get; set; } = 5;

    /// <summary>
    /// Minimum number of consecutive samples to confirm a phase transition.
    /// </summary>
    public int MinPhaseConfirmationSamples { get; set; } = 3;

    /// <summary>
    /// Altitude change rate threshold for aircraft climb detection (m/s).
    /// Negative velD indicates ascending.
    /// </summary>
    public double AircraftClimbThreshold { get; set; } = -2.0;

    /// <summary>
    /// Minimum altitude change for exit detection (meters).
    /// Exit is at or near peak altitude.
    /// </summary>
    public double ExitAltitudeWindow { get; set; } = 50.0;
}
