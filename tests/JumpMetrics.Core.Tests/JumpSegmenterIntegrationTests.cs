using JumpMetrics.Core.Models;
using JumpMetrics.Core.Services.Segmentation;

namespace JumpMetrics.Core.Tests;

/// <summary>
/// Integration tests for JumpSegmenter using realistic FlySight data patterns
/// based on the sample-jump.csv profile described in CLAUDE.md
/// </summary>
public class JumpSegmenterIntegrationTests
{
    [Fact]
    public void Segment_RealisticHopAndPopProfile_DetectsExpectedPhases()
    {
        // Based on the real sample data profile:
        // - GPS acquisition (poor accuracy)
        // - Aircraft climb (960m -> 1,910m MSL, negative velD)
        // - Exit (~1,910m)
        // - Short freefall (~15 sec, peak velD ~25 m/s)
        // - Deployment (~1,780m -> ~1,740m, sharp decel)
        // - Canopy flight (~4 min, steady descent 3-8 m/s)
        // - Landing (~193m MSL)

        var segmenter = new JumpSegmenter();
        var dataPoints = CreateRealisticHopAndPopData();

        var result = segmenter.Segment(dataPoints);

        // Verify we detected the major phases
        Assert.NotEmpty(result);
        
        var aircraftSegment = result.FirstOrDefault(s => s.Type == SegmentType.Aircraft);
        Assert.NotNull(aircraftSegment);
        Assert.True(aircraftSegment.StartAltitude < aircraftSegment.EndAltitude, "Aircraft should be climbing");
        
        var freefallSegment = result.FirstOrDefault(s => s.Type == SegmentType.Freefall);
        Assert.NotNull(freefallSegment);
        Assert.True(freefallSegment.Duration > 5, "Freefall should be at least 5 seconds");
        Assert.True(freefallSegment.Duration < 30, "Hop-n-pop freefall should be less than 30 seconds");
        
        var canopySegment = result.FirstOrDefault(s => s.Type == SegmentType.Canopy);
        Assert.NotNull(canopySegment);
        Assert.True(canopySegment.Duration > 60, "Canopy flight should be over a minute");
        
        var landingSegment = result.FirstOrDefault(s => s.Type == SegmentType.Landing);
        Assert.NotNull(landingSegment);
        Assert.True(landingSegment.EndAltitude < 250, "Landing should be near ground level");
    }

    [Fact]
    public void Segment_FullAltitudeJump_DetectsLongerFreefall()
    {
        var segmenter = new JumpSegmenter();
        var dataPoints = CreateFullAltitudeJumpData();

        var result = segmenter.Segment(dataPoints);

        var freefallSegment = result.FirstOrDefault(s => s.Type == SegmentType.Freefall);
        Assert.NotNull(freefallSegment);
        Assert.True(freefallSegment.Duration > 30, "Full altitude freefall should exceed 30 seconds");
        
        var maxVelD = freefallSegment.DataPoints.Max(dp => dp.VelocityDown);
        Assert.True(maxVelD > 50, "Terminal velocity should exceed 50 m/s");
    }

    [Fact]
    public void Segment_TurbulentCanopyFlight_DoesNotMisidentifyAsDeployment()
    {
        // Canopy flight with turns and aggressive maneuvers
        // should not be misidentified as multiple deployment events
        
        var segmenter = new JumpSegmenter();
        var dataPoints = CreateTurbulentCanopyData();

        var result = segmenter.Segment(dataPoints);

        var deploymentSegments = result.Where(s => s.Type == SegmentType.Deployment).ToList();
        Assert.True(deploymentSegments.Count <= 1, "Should not detect multiple deployments");
        
        var canopySegment = result.FirstOrDefault(s => s.Type == SegmentType.Canopy);
        Assert.NotNull(canopySegment);
    }

    private List<DataPoint> CreateRealisticHopAndPopData()
    {
        var dataPoints = new List<DataPoint>();
        var baseTime = DateTime.UtcNow;
        double currentTime = 0;
        const double sampleRate = 0.2; // 5 Hz

        // GPS acquisition (10 seconds, poor accuracy, altitude jumping)
        for (int i = 0; i < 50; i++)
        {
            dataPoints.Add(new DataPoint
            {
                Time = baseTime.AddSeconds(currentTime),
                AltitudeMSL = 960 + (i % 3) * 20, // Noisy altitude
                VelocityDown = -8 + (i % 2) * 3,
                VelocityNorth = 50 + (i % 2) * 5,
                VelocityEast = 10,
                HorizontalAccuracy = 150 - (i * 2), // Improving accuracy
                NumberOfSatellites = Math.Min(4 + i / 10, 10)
            });
            currentTime += sampleRate;
        }

        // Aircraft climb (120 seconds = 600 samples)
        double altitude = 960;
        for (int i = 0; i < 600; i++)
        {
            altitude += 1.6; // Climbing ~950m in 120 seconds
            dataPoints.Add(new DataPoint
            {
                Time = baseTime.AddSeconds(currentTime),
                AltitudeMSL = altitude,
                VelocityDown = -4.8 + Math.Sin(i * 0.1) * 0.5, // Steady climb with small variations
                VelocityNorth = 52 + Math.Sin(i * 0.05) * 3,
                VelocityEast = 12,
                HorizontalAccuracy = 15,
                NumberOfSatellites = 9
            });
            currentTime += sampleRate;
        }

        // Exit transition (2 seconds)
        for (int i = 0; i < 10; i++)
        {
            dataPoints.Add(new DataPoint
            {
                Time = baseTime.AddSeconds(currentTime),
                AltitudeMSL = 1910 - (i * 0.5),
                VelocityDown = (i * 2.0), // Transitioning from negative to positive
                VelocityNorth = 52 - (i * 4),
                VelocityEast = 12 - (i * 1),
                HorizontalAccuracy = 12,
                NumberOfSatellites = 10
            });
            currentTime += sampleRate;
        }

        // Freefall (15 seconds = 75 samples, reaching ~25 m/s)
        for (int i = 0; i < 75; i++)
        {
            double velD = Math.Min(20 + (i * 0.3), 25);
            altitude -= velD * sampleRate;
            dataPoints.Add(new DataPoint
            {
                Time = baseTime.AddSeconds(currentTime),
                AltitudeMSL = altitude,
                VelocityDown = velD,
                VelocityNorth = 5 + Math.Sin(i * 0.1) * 2,
                VelocityEast = 3,
                HorizontalAccuracy = 10,
                NumberOfSatellites = 10
            });
            currentTime += sampleRate;
        }

        // Deployment (3 seconds = 15 samples, decel from 25 to 5 m/s)
        double deployVelD = 25;
        for (int i = 0; i < 15; i++)
        {
            deployVelD = 25 - (i / 15.0) * 20;
            altitude -= deployVelD * sampleRate;
            dataPoints.Add(new DataPoint
            {
                Time = baseTime.AddSeconds(currentTime),
                AltitudeMSL = altitude,
                VelocityDown = deployVelD,
                VelocityNorth = 5,
                VelocityEast = 3,
                HorizontalAccuracy = 10,
                NumberOfSatellites = 10
            });
            currentTime += sampleRate;
        }

        // Canopy flight (240 seconds = 1200 samples)
        for (int i = 0; i < 1200; i++)
        {
            double canopyVelD = 5 + Math.Sin(i * 0.05) * 2; // 3-7 m/s with oscillations
            altitude -= canopyVelD * sampleRate;
            dataPoints.Add(new DataPoint
            {
                Time = baseTime.AddSeconds(currentTime),
                AltitudeMSL = Math.Max(193, altitude),
                VelocityDown = canopyVelD,
                VelocityNorth = 8 + Math.Sin(i * 0.1) * 3,
                VelocityEast = 6 + Math.Cos(i * 0.08) * 2,
                HorizontalAccuracy = 10,
                NumberOfSatellites = 10
            });
            currentTime += sampleRate;
        }

        // Landing (13 seconds on ground)
        for (int i = 0; i < 65; i++)
        {
            double landingVelD = Math.Max(0, 2 - (i * 0.04));
            double horizontalSpeed = Math.Max(0, 4 - (i * 0.08));
            dataPoints.Add(new DataPoint
            {
                Time = baseTime.AddSeconds(currentTime),
                AltitudeMSL = 193,
                VelocityDown = landingVelD,
                VelocityNorth = horizontalSpeed,
                VelocityEast = 0,
                HorizontalAccuracy = 10,
                NumberOfSatellites = 10
            });
            currentTime += sampleRate;
        }

        return dataPoints;
    }

    private List<DataPoint> CreateFullAltitudeJumpData()
    {
        var dataPoints = new List<DataPoint>();
        var baseTime = DateTime.UtcNow;
        double currentTime = 0;
        const double sampleRate = 0.2;

        // Aircraft climb to 13,000 ft (4,000m)
        double altitude = 1000;
        for (int i = 0; i < 900; i++)
        {
            altitude += 3.3;
            dataPoints.Add(new DataPoint
            {
                Time = baseTime.AddSeconds(currentTime),
                AltitudeMSL = altitude,
                VelocityDown = -5,
                VelocityNorth = 50,
                VelocityEast = 10,
                HorizontalAccuracy = 15,
                NumberOfSatellites = 9
            });
            currentTime += sampleRate;
        }

        // Exit
        for (int i = 0; i < 10; i++)
        {
            dataPoints.Add(new DataPoint
            {
                Time = baseTime.AddSeconds(currentTime),
                AltitudeMSL = 4000,
                VelocityDown = i * 2,
                VelocityNorth = 50 - (i * 4),
                VelocityEast = 10,
                HorizontalAccuracy = 12,
                NumberOfSatellites = 10
            });
            currentTime += sampleRate;
        }

        // Freefall (60 seconds, reaching terminal velocity ~55 m/s)
        for (int i = 0; i < 300; i++)
        {
            double velD = Math.Min(20 + (i * 0.3), 55);
            altitude -= velD * sampleRate;
            dataPoints.Add(new DataPoint
            {
                Time = baseTime.AddSeconds(currentTime),
                AltitudeMSL = altitude,
                VelocityDown = velD,
                VelocityNorth = 5,
                VelocityEast = 3,
                HorizontalAccuracy = 10,
                NumberOfSatellites = 10
            });
            currentTime += sampleRate;
        }

        // Deployment and canopy (simplified)
        for (int i = 0; i < 15; i++)
        {
            double velD = 55 - (i / 15.0) * 50;
            altitude -= velD * sampleRate;
            dataPoints.Add(new DataPoint
            {
                Time = baseTime.AddSeconds(currentTime),
                AltitudeMSL = altitude,
                VelocityDown = velD,
                VelocityNorth = 5,
                VelocityEast = 3,
                HorizontalAccuracy = 10,
                NumberOfSatellites = 10
            });
            currentTime += sampleRate;
        }

        for (int i = 0; i < 500; i++)
        {
            double velD = 6;
            altitude -= velD * sampleRate;
            dataPoints.Add(new DataPoint
            {
                Time = baseTime.AddSeconds(currentTime),
                AltitudeMSL = Math.Max(200, altitude),
                VelocityDown = velD,
                VelocityNorth = 8,
                VelocityEast = 6,
                HorizontalAccuracy = 10,
                NumberOfSatellites = 10
            });
            currentTime += sampleRate;
        }

        return dataPoints;
    }

    private List<DataPoint> CreateTurbulentCanopyData()
    {
        var dataPoints = new List<DataPoint>();
        var baseTime = DateTime.UtcNow;
        double currentTime = 0;
        const double sampleRate = 0.2;

        // Start under canopy at 1500m
        double altitude = 1500;

        // Canopy flight with aggressive turns and speed changes
        for (int i = 0; i < 600; i++)
        {
            // Simulate swooping/turning with rapid velocity changes
            double velD;
            if (i % 100 < 10) // Aggressive descent
            {
                velD = 12 + Math.Sin(i * 0.5) * 3;
            }
            else if (i % 100 < 20) // Flare/braking
            {
                velD = 2 + Math.Sin(i * 0.3);
            }
            else // Normal flight
            {
                velD = 5 + Math.Sin(i * 0.1) * 2;
            }

            altitude -= velD * sampleRate;
            dataPoints.Add(new DataPoint
            {
                Time = baseTime.AddSeconds(currentTime),
                AltitudeMSL = Math.Max(200, altitude),
                VelocityDown = velD,
                VelocityNorth = 10 + Math.Sin(i * 0.15) * 8,
                VelocityEast = 5 + Math.Cos(i * 0.12) * 6,
                HorizontalAccuracy = 10,
                NumberOfSatellites = 10
            });
            currentTime += sampleRate;
        }

        return dataPoints;
    }
}
