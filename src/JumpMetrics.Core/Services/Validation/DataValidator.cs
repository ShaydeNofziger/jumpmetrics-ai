using JumpMetrics.Core.Interfaces;
using JumpMetrics.Core.Models;

namespace JumpMetrics.Core.Services.Validation;

public class DataValidator : IDataValidator
{
    private const int MinDataPoints = 10;
    private const double MaxGpsAccuracy = 50.0; // meters
    private const int MinSatellites = 6;
    private const double MaxTimeGap = 2.0; // seconds
    private const double MinAltitude = -100.0; // meters MSL
    private const double MaxAltitude = 10000.0; // meters MSL
    private const double MaxVelocityDown = 150.0; // m/s

    public ValidationResult Validate(IReadOnlyList<DataPoint> dataPoints)
    {
        var result = new ValidationResult { IsValid = true };

        // Error checks (make IsValid = false)
        if (dataPoints == null || dataPoints.Count == 0)
        {
            result.IsValid = false;
            result.Errors.Add("No data points provided");
            return result;
        }

        if (dataPoints.Count < MinDataPoints)
        {
            result.IsValid = false;
            result.Errors.Add($"Insufficient data points: {dataPoints.Count} (minimum {MinDataPoints} required)");
            return result;
        }

        // Check for time progression
        var uniqueTimes = dataPoints.Select(dp => dp.Time).Distinct().Count();
        if (uniqueTimes == 1)
        {
            result.IsValid = false;
            result.Errors.Add("All timestamps are identical - no time progression detected");
            return result;
        }

        // Warning checks (IsValid stays true, but issues are flagged)
        
        // Check GPS accuracy
        var poorAccuracyCount = dataPoints.Count(dp => dp.HorizontalAccuracy > MaxGpsAccuracy);
        if (poorAccuracyCount > 0)
        {
            result.Warnings.Add($"{poorAccuracyCount} data points have poor GPS accuracy (hAcc > {MaxGpsAccuracy}m)");
        }

        // Check satellite count
        var lowSatelliteCount = dataPoints.Count(dp => dp.NumberOfSatellites < MinSatellites);
        if (lowSatelliteCount > 0)
        {
            result.Warnings.Add($"{lowSatelliteCount} data points have insufficient satellites (numSV < {MinSatellites})");
        }

        // Check for monotonically increasing timestamps
        for (int i = 1; i < dataPoints.Count; i++)
        {
            if (dataPoints[i].Time < dataPoints[i - 1].Time)
            {
                result.Warnings.Add("Timestamps are not monotonically increasing - data may be out of order");
                break;
            }
        }

        // Check for time gaps
        for (int i = 1; i < dataPoints.Count; i++)
        {
            var gap = (dataPoints[i].Time - dataPoints[i - 1].Time).TotalSeconds;
            if (gap > MaxTimeGap)
            {
                result.Warnings.Add($"Large time gap detected: {gap:F1}s between data points (>{MaxTimeGap}s threshold)");
                break; // Only report once
            }
        }

        // Check altitude values
        var invalidAltitudeCount = dataPoints.Count(dp => dp.AltitudeMSL < MinAltitude || dp.AltitudeMSL > MaxAltitude);
        if (invalidAltitudeCount > 0)
        {
            result.Warnings.Add($"{invalidAltitudeCount} data points have altitude outside reasonable range ({MinAltitude}m to {MaxAltitude}m MSL)");
        }

        // Check velocity values
        var implausibleVelocityCount = dataPoints.Count(dp => Math.Abs(dp.VelocityDown) > MaxVelocityDown);
        if (implausibleVelocityCount > 0)
        {
            result.Warnings.Add($"{implausibleVelocityCount} data points have implausible velocity (|velD| > {MaxVelocityDown}m/s)");
        }

        return result;
    }
}
