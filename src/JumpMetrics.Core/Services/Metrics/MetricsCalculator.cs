using JumpMetrics.Core.Interfaces;
using JumpMetrics.Core.Models;

namespace JumpMetrics.Core.Services.Metrics;

public class MetricsCalculator : IMetricsCalculator
{
    public JumpPerformanceMetrics Calculate(IReadOnlyList<JumpSegment> segments)
    {
        var metrics = new JumpPerformanceMetrics();

        // Find segments by type
        var freefallSegment = segments.FirstOrDefault(s => s.Type == SegmentType.Freefall);
        var canopySegment = segments.FirstOrDefault(s => s.Type == SegmentType.Canopy);
        var landingSegment = segments.FirstOrDefault(s => s.Type == SegmentType.Landing);

        // Calculate metrics for each phase
        metrics.Freefall = CalculateFreefallMetrics(freefallSegment);
        metrics.Canopy = CalculateCanopyMetrics(canopySegment);
        metrics.Landing = CalculateLandingMetrics(landingSegment);

        return metrics;
    }

    private FreefallMetrics? CalculateFreefallMetrics(JumpSegment? segment)
    {
        if (segment == null || segment.DataPoints.Count == 0)
        {
            return null;
        }

        var dataPoints = segment.DataPoints;
        
        // Calculate average vertical speed (mean of VelocityDown)
        var averageVerticalSpeed = dataPoints.Average(dp => dp.VelocityDown);
        
        // Calculate max vertical speed
        var maxVerticalSpeed = dataPoints.Max(dp => dp.VelocityDown);
        
        // Calculate average horizontal speed (mean of HorizontalSpeed property)
        var averageHorizontalSpeed = dataPoints.Average(dp => dp.HorizontalSpeed);
        
        // Calculate track angle (average ground track)
        // GroundTrack is in radians, convert to degrees
        var trackAngleRadians = dataPoints.Average(dp => dp.GroundTrack);
        var trackAngle = trackAngleRadians * 180.0 / Math.PI;
        
        // Time in freefall is the segment duration
        var timeInFreefall = segment.Duration;

        return new FreefallMetrics
        {
            AverageVerticalSpeed = averageVerticalSpeed,
            MaxVerticalSpeed = maxVerticalSpeed,
            AverageHorizontalSpeed = averageHorizontalSpeed,
            TrackAngle = trackAngle,
            TimeInFreefall = timeInFreefall
        };
    }

    private CanopyMetrics? CalculateCanopyMetrics(JumpSegment? segment)
    {
        if (segment == null || segment.DataPoints.Count == 0)
        {
            return null;
        }

        var dataPoints = segment.DataPoints;
        
        // Deployment altitude is the start altitude of the canopy segment
        var deploymentAltitude = segment.StartAltitude;
        
        // Calculate average descent rate (mean of VelocityDown under canopy)
        var averageDescentRate = dataPoints.Average(dp => dp.VelocityDown);
        
        // Calculate max horizontal speed
        var maxHorizontalSpeed = dataPoints.Max(dp => dp.HorizontalSpeed);
        
        // Total canopy time is the segment duration
        var totalCanopyTime = segment.Duration;
        
        // Calculate glide ratio: total horizontal distance / altitude lost
        double totalHorizontalDistance = 0.0;
        for (int i = 1; i < dataPoints.Count; i++)
        {
            var dt = (dataPoints[i].Time - dataPoints[i - 1].Time).TotalSeconds;
            var horizontalSpeed = dataPoints[i].HorizontalSpeed;
            totalHorizontalDistance += horizontalSpeed * dt;
        }
        
        var altitudeLost = segment.StartAltitude - segment.EndAltitude;
        var glideRatio = altitudeLost > 0 ? totalHorizontalDistance / altitudeLost : 0.0;
        
        // Calculate pattern altitude (optional)
        // Heuristic: detect sustained turn sequence below 300m AGL
        // For now, we'll use a simple approach: find minimum altitude as ground level
        var minAltitude = dataPoints.Min(dp => dp.AltitudeMSL);
        var patternAltitude = DetectPatternAltitude(dataPoints, minAltitude);

        return new CanopyMetrics
        {
            DeploymentAltitude = deploymentAltitude,
            AverageDescentRate = averageDescentRate,
            GlideRatio = glideRatio,
            MaxHorizontalSpeed = maxHorizontalSpeed,
            TotalCanopyTime = totalCanopyTime,
            PatternAltitude = patternAltitude
        };
    }

    private double? DetectPatternAltitude(List<DataPoint> dataPoints, double groundElevation)
    {
        // Pattern altitude detection: Look for sustained turning below 300m AGL
        // This is a simplified heuristic - detect significant changes in ground track
        const double patternAglThreshold = 300.0; // meters AGL
        
        for (int i = 10; i < dataPoints.Count - 10; i++)
        {
            var altitude = dataPoints[i].AltitudeMSL;
            var agl = altitude - groundElevation;
            
            if (agl < patternAglThreshold)
            {
                // Check for turn pattern (change in ground track)
                var windowSize = Math.Min(10, dataPoints.Count - i - 1);
                if (windowSize >= 5)
                {
                    var trackChanges = new List<double>();
                    for (int j = i; j < i + windowSize - 1; j++)
                    {
                        var trackChange = Math.Abs(dataPoints[j + 1].GroundTrack - dataPoints[j].GroundTrack);
                        // Handle wrap-around at ±π
                        if (trackChange > Math.PI)
                        {
                            trackChange = 2 * Math.PI - trackChange;
                        }
                        trackChanges.Add(trackChange);
                    }
                    
                    // If average track change indicates turning, this could be pattern entry
                    var avgTrackChange = trackChanges.Average();
                    if (avgTrackChange > 0.05) // ~3 degrees per sample (rough heuristic)
                    {
                        return altitude;
                    }
                }
            }
        }
        
        // If no clear pattern detected, return null
        return null;
    }

    private LandingMetrics? CalculateLandingMetrics(JumpSegment? segment)
    {
        if (segment == null || segment.DataPoints.Count == 0)
        {
            return null;
        }

        var dataPoints = segment.DataPoints;
        
        // Final approach speed: average horizontal speed over last 10 seconds
        var approachDuration = TimeSpan.FromSeconds(10);
        var approachStartTime = segment.EndTime - approachDuration;
        var approachPoints = dataPoints.Where(dp => dp.Time >= approachStartTime).ToList();
        
        var finalApproachSpeed = approachPoints.Any() 
            ? approachPoints.Average(dp => dp.HorizontalSpeed)
            : dataPoints.Average(dp => dp.HorizontalSpeed);
        
        // Touchdown vertical speed: VelocityDown at last data point
        var touchdownVerticalSpeed = dataPoints.Last().VelocityDown;
        
        // Landing accuracy: null for now (requires target coordinate)
        double? landingAccuracy = null;

        return new LandingMetrics
        {
            FinalApproachSpeed = finalApproachSpeed,
            TouchdownVerticalSpeed = touchdownVerticalSpeed,
            LandingAccuracy = landingAccuracy
        };
    }
}
