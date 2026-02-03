using JumpMetrics.Core.Interfaces;
using JumpMetrics.Core.Models;

namespace JumpMetrics.Core.Services.Segmentation;

public class JumpSegmenter : IJumpSegmenter
{
    private readonly SegmentationOptions _options;

    // Algorithm constants
    private const int DeploymentLookbackSamples = 5;
    private const double DeploymentVelocityRelaxationFactor = 0.8;

    public JumpSegmenter()
        : this(new SegmentationOptions())
    {
    }

    public JumpSegmenter(SegmentationOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public IReadOnlyList<JumpSegment> Segment(IReadOnlyList<DataPoint> dataPoints)
    {
        if (dataPoints == null || dataPoints.Count == 0)
        {
            return Array.Empty<JumpSegment>();
        }

        var segments = new List<JumpSegment>();
        var goodPoints = FilterGoodDataPoints(dataPoints);

        if (goodPoints.Count < _options.SmoothingWindowSize)
        {
            return Array.Empty<JumpSegment>();
        }

        var smoothedVelD = CalculateSmoothedVelocity(goodPoints);
        var peakAltitudeIndex = FindPeakAltitudeIndex(goodPoints);

        int currentIndex = 0;

        var aircraftSegment = DetectAircraftPhase(goodPoints, smoothedVelD, peakAltitudeIndex, ref currentIndex);
        if (aircraftSegment != null)
        {
            segments.Add(aircraftSegment);
        }

        var exitIndex = currentIndex;
        var freefallSegment = DetectFreefallPhase(goodPoints, smoothedVelD, ref currentIndex);
        if (freefallSegment != null)
        {
            // Find the actual start of freefall from the segment's data points, if available
            if (freefallSegment.DataPoints != null && freefallSegment.DataPoints.Count > 0)
            {
                int freefallStartIndex = goodPoints.IndexOf(freefallSegment.DataPoints[0]);
                if (freefallStartIndex == -1)
                {
                    throw new System.InvalidOperationException(
                        "Freefall segment start point was not found in the source data points. " +
                        "This indicates an internal inconsistency in jump segmentation.");
                }
                var exitSegment = CreateExitSegment(goodPoints, exitIndex, freefallStartIndex);
                if (exitSegment != null)
                {
                    segments.Add(exitSegment);
                }
            }
            segments.Add(freefallSegment);
        }

        var deploymentSegment = DetectDeploymentPhase(goodPoints, smoothedVelD, ref currentIndex);
        if (deploymentSegment != null)
        {
            segments.Add(deploymentSegment);
        }

        var canopySegment = DetectCanopyPhase(goodPoints, smoothedVelD, ref currentIndex);
        if (canopySegment != null)
        {
            segments.Add(canopySegment);
        }

        var landingSegment = DetectLandingPhase(goodPoints, ref currentIndex);
        if (landingSegment != null)
        {
            segments.Add(landingSegment);
        }

        return segments;
    }

    private List<DataPoint> FilterGoodDataPoints(IReadOnlyList<DataPoint> dataPoints)
    {
        return dataPoints
            .Where(dp => dp.HorizontalAccuracy <= _options.GpsAccuracyThreshold)
            .ToList();
    }

    private List<double> CalculateSmoothedVelocity(List<DataPoint> dataPoints)
    {
        var smoothed = new List<double>(dataPoints.Count);
        int windowSize = _options.SmoothingWindowSize;

        for (int i = 0; i < dataPoints.Count; i++)
        {
            int start = Math.Max(0, i - windowSize / 2);
            int end = Math.Min(dataPoints.Count, i + windowSize / 2 + 1);
            double sum = 0;
            int count = 0;

            for (int j = start; j < end; j++)
            {
                sum += dataPoints[j].VelocityDown;
                count++;
            }

            smoothed.Add(count > 0 ? sum / count : dataPoints[i].VelocityDown);
        }

        return smoothed;
    }

    private int FindPeakAltitudeIndex(List<DataPoint> dataPoints)
    {
        double maxAlt = double.MinValue;
        int maxIndex = 0;

        for (int i = 0; i < dataPoints.Count; i++)
        {
            if (dataPoints[i].AltitudeMSL > maxAlt)
            {
                maxAlt = dataPoints[i].AltitudeMSL;
                maxIndex = i;
            }
        }

        return maxIndex;
    }

    private JumpSegment? DetectAircraftPhase(
        List<DataPoint> dataPoints,
        List<double> smoothedVelD,
        int peakAltitudeIndex,
        ref int currentIndex)
    {
        int startIndex = currentIndex;
        int aircraftEndIndex = -1;

        for (int i = currentIndex; i <= peakAltitudeIndex && i < dataPoints.Count - 1; i++)
        {
            if (smoothedVelD[i] < _options.AircraftClimbThreshold ||
                (i < peakAltitudeIndex && dataPoints[i].AltitudeMSL < dataPoints[i + 1].AltitudeMSL))
            {
                aircraftEndIndex = i;
            }
            else
            {
                break;
            }
        }

        if (aircraftEndIndex > startIndex)
        {
            currentIndex = aircraftEndIndex + 1;
            return CreateSegment(SegmentType.Aircraft, dataPoints, startIndex, aircraftEndIndex);
        }

        return null;
    }

    private JumpSegment CreateExitSegment(List<DataPoint> dataPoints, int exitIndex, int freefallStartIndex)
    {
        if (exitIndex >= freefallStartIndex)
        {
            return null!;
        }

        int exitEndIndex = Math.Max(exitIndex, Math.Min(exitIndex + _options.MinPhaseConfirmationSamples, freefallStartIndex - 1));
        return CreateSegment(SegmentType.Exit, dataPoints, exitIndex, exitEndIndex);
    }

    private JumpSegment? DetectFreefallPhase(
        List<DataPoint> dataPoints,
        List<double> smoothedVelD,
        ref int currentIndex)
    {
        int searchStartIndex = currentIndex;
        int freefallStartIndex = -1;
        int freefallEndIndex = -1;

        // First, find where freefall actually begins (velD first exceeds minimum)
        for (int i = searchStartIndex; i < dataPoints.Count - _options.MinPhaseConfirmationSamples; i++)
        {
            if (smoothedVelD[i] >= _options.MinFreefallVelD)
            {
                freefallStartIndex = i;
                break;
            }
        }

        if (freefallStartIndex == -1)
        {
            return null;  // Never reached freefall speeds
        }

        // Now track how long freefall continues
        int consecutiveNonFreefallSamples = 0;
        for (int i = freefallStartIndex; i < dataPoints.Count - _options.MinPhaseConfirmationSamples; i++)
        {
            bool isAccelerating = false;
            bool meetsMinVelD = smoothedVelD[i] >= _options.MinFreefallVelD;

            if (i < dataPoints.Count - 1)
            {
                double velDChange = smoothedVelD[i + 1] - smoothedVelD[i];
                // Only consider it accelerating if velocity is actually increasing
                // OR if it's maintaining high freefall speed (above max canopy speed).
                // Note: The 10-15 m/s range is intentionally excluded when velocity is decreasing,
                // as this indicates deployment is occurring and freefall should end.
                isAccelerating = velDChange > 0 || smoothedVelD[i] > _options.MaxCanopyVelD;
            }

            if (meetsMinVelD && isAccelerating)
            {
                freefallEndIndex = i;
                consecutiveNonFreefallSamples = 0;  // Reset counter
            }
            else if (freefallEndIndex > freefallStartIndex)
            {
                consecutiveNonFreefallSamples++;
                
                // Check if there's a deployment transition in the recent past (look back a few samples)
                // This catches cases where deployment started before velocity dropped below threshold
                bool isDeployment = false;
                for (int lookBack = 0; lookBack <= Math.Min(DeploymentLookbackSamples, i - freefallStartIndex); lookBack++)
                {
                    if (IsDeploymentTransition(smoothedVelD, i - lookBack))
                    {
                        isDeployment = true;
                        break;
                    }
                }
                
                if (isDeployment)
                {
                    break;
                }
                
                // If we've had several consecutive samples not meeting freefall criteria, end freefall
                // This handles cases where deployment detection might fail but freefall has clearly ended
                if (consecutiveNonFreefallSamples >= _options.MinPhaseConfirmationSamples)
                {
                    break;
                }
            }
        }

        if (freefallEndIndex > freefallStartIndex)
        {
            currentIndex = freefallEndIndex + 1;
            return CreateSegment(SegmentType.Freefall, dataPoints, freefallStartIndex, freefallEndIndex);
        }

        return null;
    }

    private JumpSegment? DetectDeploymentPhase(
        List<DataPoint> dataPoints,
        List<double> smoothedVelD,
        ref int currentIndex)
    {
        int startIndex = currentIndex;
        int deploymentStartIndex = -1;
        int deploymentEndIndex = -1;

        // Search for deployment starting a few samples before currentIndex (in case deployment
        // signature was just before freefall ended) and continuing forward
        int searchStart = Math.Max(0, currentIndex - DeploymentLookbackSamples);
        for (int i = searchStart; i < dataPoints.Count - _options.MinPhaseConfirmationSamples; i++)
        {
            if (IsDeploymentTransition(smoothedVelD, i))
            {
                deploymentStartIndex = i;
                deploymentEndIndex = i + (_options.MinPhaseConfirmationSamples * 2);
                break;
            }
        }

        if (deploymentStartIndex >= 0 && deploymentEndIndex > deploymentStartIndex)
        {
            deploymentEndIndex = Math.Min(deploymentEndIndex, dataPoints.Count - 1);
            currentIndex = deploymentEndIndex;
            return CreateSegment(SegmentType.Deployment, dataPoints, deploymentStartIndex, deploymentEndIndex);
        }

        return null;
    }

    private bool IsDeploymentTransition(List<double> smoothedVelD, int index)
    {
        int lookAheadSamples = Math.Min(10, smoothedVelD.Count - index - 1);
        
        if (lookAheadSamples < 3)
        {
            return false;
        }

        double initialVelD = smoothedVelD[index];
        double finalVelD = smoothedVelD[index + lookAheadSamples];
        
        double velDChange = initialVelD - finalVelD;
        double deceleration = velDChange / lookAheadSamples;

        // Check if this is a significant deceleration from freefall speeds to canopy speeds
        bool hasSignificantDecel = velDChange > (lookAheadSamples * _options.DeploymentDecelThreshold);
        // Start velocity should be at least moderate (allow slightly below MinFreefallVelD for cases where
        // freefall has just ended and deceleration is beginning)
        bool startsHigh = initialVelD >= (_options.MinFreefallVelD * DeploymentVelocityRelaxationFactor);
        bool endsLow = finalVelD <= _options.MaxCanopyVelD;
        
        return hasSignificantDecel && startsHigh && endsLow;
    }

    private JumpSegment? DetectCanopyPhase(
        List<DataPoint> dataPoints,
        List<double> smoothedVelD,
        ref int currentIndex)
    {
        int startIndex = currentIndex;
        int canopyEndIndex = -1;

        for (int i = currentIndex; i < dataPoints.Count; i++)
        {
            double velD = smoothedVelD[i];

            if (velD >= _options.MinCanopyVelD && velD <= _options.MaxCanopyVelD)
            {
                canopyEndIndex = i;
            }
            else if (canopyEndIndex > startIndex && velD < _options.MinCanopyVelD)
            {
                break;
            }
        }

        if (canopyEndIndex > startIndex)
        {
            currentIndex = canopyEndIndex + 1;
            return CreateSegment(SegmentType.Canopy, dataPoints, startIndex, canopyEndIndex);
        }

        return null;
    }

    private JumpSegment? DetectLandingPhase(
        List<DataPoint> dataPoints,
        ref int currentIndex)
    {
        int startIndex = currentIndex;

        if (startIndex >= dataPoints.Count)
        {
            return null;
        }

        double minAltitude = dataPoints.Skip(startIndex).Min(dp => dp.AltitudeMSL);
        int landingStartIndex = -1;

        for (int i = startIndex; i < dataPoints.Count; i++)
        {
            bool nearGround = Math.Abs(dataPoints[i].AltitudeMSL - minAltitude) < 10.0;
            bool lowVelD = dataPoints[i].VelocityDown < _options.LandingVelDThreshold;
            bool lowHorizontalSpeed = dataPoints[i].HorizontalSpeed < _options.LandingHorizontalThreshold;

            if (nearGround && lowVelD && lowHorizontalSpeed)
            {
                landingStartIndex = i;
                break;
            }
        }

        if (landingStartIndex >= startIndex)
        {
            currentIndex = dataPoints.Count;
            return CreateSegment(SegmentType.Landing, dataPoints, landingStartIndex, dataPoints.Count - 1);
        }

        return null;
    }

    private JumpSegment CreateSegment(
        SegmentType type,
        List<DataPoint> dataPoints,
        int startIndex,
        int endIndex)
    {
        startIndex = Math.Max(0, startIndex);
        endIndex = Math.Min(dataPoints.Count - 1, endIndex);

        var segmentPoints = dataPoints.Skip(startIndex).Take(endIndex - startIndex + 1).ToList();

        return new JumpSegment
        {
            Type = type,
            StartTime = dataPoints[startIndex].Time,
            EndTime = dataPoints[endIndex].Time,
            StartAltitude = dataPoints[startIndex].AltitudeMSL,
            EndAltitude = dataPoints[endIndex].AltitudeMSL,
            DataPoints = segmentPoints
        };
    }
}
