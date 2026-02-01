using JumpMetrics.Core.Models;
using JumpMetrics.Core.Services.Metrics;

namespace JumpMetrics.Core.Tests;

public class MetricsCalculatorTests
{
    [Fact]
    public void Calculate_EmptySegments_ReturnsMetricsWithNullValues()
    {
        var calculator = new MetricsCalculator();

        var result = calculator.Calculate([]);

        Assert.NotNull(result);
        Assert.Null(result.Freefall);
        Assert.Null(result.Canopy);
        Assert.Null(result.Landing);
    }

    [Fact]
    public void Calculate_FreefallSegment_CalculatesCorrectMetrics()
    {
        var calculator = new MetricsCalculator();
        
        var dataPoints = new List<DataPoint>
        {
            new() { Time = DateTime.UtcNow, VelocityDown = 10.0, VelocityNorth = 5.0, VelocityEast = 3.0, AltitudeMSL = 2000 },
            new() { Time = DateTime.UtcNow.AddSeconds(1), VelocityDown = 20.0, VelocityNorth = 6.0, VelocityEast = 4.0, AltitudeMSL = 1990 },
            new() { Time = DateTime.UtcNow.AddSeconds(2), VelocityDown = 30.0, VelocityNorth = 7.0, VelocityEast = 5.0, AltitudeMSL = 1970 },
            new() { Time = DateTime.UtcNow.AddSeconds(3), VelocityDown = 40.0, VelocityNorth = 8.0, VelocityEast = 6.0, AltitudeMSL = 1940 }
        };
        
        var segment = new JumpSegment
        {
            Type = SegmentType.Freefall,
            StartTime = dataPoints[0].Time,
            EndTime = dataPoints[^1].Time,
            StartAltitude = 2000,
            EndAltitude = 1940,
            DataPoints = dataPoints
        };

        var result = calculator.Calculate([segment]);

        Assert.NotNull(result.Freefall);
        Assert.Equal(25.0, result.Freefall.AverageVerticalSpeed); // (10+20+30+40)/4
        Assert.Equal(40.0, result.Freefall.MaxVerticalSpeed);
        Assert.Equal(3.0, result.Freefall.TimeInFreefall, precision: 5); // Allow for DateTime precision
        Assert.True(result.Freefall.AverageHorizontalSpeed > 0);
        Assert.Null(result.Canopy);
        Assert.Null(result.Landing);
    }

    [Fact]
    public void Calculate_CanopySegment_CalculatesGlideRatio()
    {
        var calculator = new MetricsCalculator();
        var startTime = DateTime.UtcNow;
        
        // Create canopy flight data with known horizontal distance and altitude loss
        var dataPoints = new List<DataPoint>
        {
            new() { Time = startTime, VelocityDown = 5.0, VelocityNorth = 10.0, VelocityEast = 0.0, AltitudeMSL = 1500 },
            new() { Time = startTime.AddSeconds(10), VelocityDown = 5.0, VelocityNorth = 10.0, VelocityEast = 0.0, AltitudeMSL = 1450 },
            new() { Time = startTime.AddSeconds(20), VelocityDown = 5.0, VelocityNorth = 10.0, VelocityEast = 0.0, AltitudeMSL = 1400 }
        };
        
        var segment = new JumpSegment
        {
            Type = SegmentType.Canopy,
            StartTime = dataPoints[0].Time,
            EndTime = dataPoints[^1].Time,
            StartAltitude = 1500,
            EndAltitude = 1400,
            DataPoints = dataPoints
        };

        var result = calculator.Calculate([segment]);

        Assert.NotNull(result.Canopy);
        Assert.Equal(1500, result.Canopy.DeploymentAltitude);
        Assert.Equal(5.0, result.Canopy.AverageDescentRate);
        Assert.Equal(20.0, result.Canopy.TotalCanopyTime);
        // Horizontal distance = 10 m/s * 20 sec = 200m, altitude lost = 100m, glide ratio = 2.0
        Assert.True(result.Canopy.GlideRatio > 1.9 && result.Canopy.GlideRatio < 2.1);
        Assert.Null(result.Freefall);
        Assert.Null(result.Landing);
    }

    [Fact]
    public void Calculate_LandingSegment_CalculatesApproachSpeed()
    {
        var calculator = new MetricsCalculator();
        var startTime = DateTime.UtcNow;
        
        // Create landing data spanning 15 seconds (to test 10-second window)
        var dataPoints = new List<DataPoint>();
        for (int i = 0; i <= 15; i++)
        {
            dataPoints.Add(new DataPoint
            {
                Time = startTime.AddSeconds(i),
                VelocityDown = 2.0 - (i * 0.1), // Decreasing vertical speed
                VelocityNorth = 5.0,
                VelocityEast = 0.0,
                AltitudeMSL = 200 - i
            });
        }
        
        var segment = new JumpSegment
        {
            Type = SegmentType.Landing,
            StartTime = dataPoints[0].Time,
            EndTime = dataPoints[^1].Time,
            StartAltitude = 200,
            EndAltitude = 185,
            DataPoints = dataPoints
        };

        var result = calculator.Calculate([segment]);

        Assert.NotNull(result.Landing);
        // Final approach speed should be average of last 10 seconds
        Assert.True(result.Landing.FinalApproachSpeed > 4.5 && result.Landing.FinalApproachSpeed < 5.5);
        // Touchdown vertical speed is from the last data point
        Assert.True(result.Landing.TouchdownVerticalSpeed < 1.0);
        Assert.Null(result.Landing.LandingAccuracy);
        Assert.Null(result.Freefall);
        Assert.Null(result.Canopy);
    }

    [Fact]
    public void Calculate_MultipleSegments_CalculatesAllMetrics()
    {
        var calculator = new MetricsCalculator();
        var startTime = DateTime.UtcNow;
        
        var freefallSegment = new JumpSegment
        {
            Type = SegmentType.Freefall,
            StartTime = startTime,
            EndTime = startTime.AddSeconds(30),
            StartAltitude = 3000,
            EndAltitude = 1500,
            DataPoints = new List<DataPoint>
            {
                new() { Time = startTime, VelocityDown = 30.0, VelocityNorth = 10.0, VelocityEast = 5.0, AltitudeMSL = 3000 },
                new() { Time = startTime.AddSeconds(15), VelocityDown = 50.0, VelocityNorth = 12.0, VelocityEast = 6.0, AltitudeMSL = 2250 },
                new() { Time = startTime.AddSeconds(30), VelocityDown = 55.0, VelocityNorth = 15.0, VelocityEast = 8.0, AltitudeMSL = 1500 }
            }
        };
        
        var canopySegment = new JumpSegment
        {
            Type = SegmentType.Canopy,
            StartTime = startTime.AddSeconds(35),
            EndTime = startTime.AddSeconds(235),
            StartAltitude = 1500,
            EndAltitude = 200,
            DataPoints = new List<DataPoint>
            {
                new() { Time = startTime.AddSeconds(35), VelocityDown = 5.0, VelocityNorth = 8.0, VelocityEast = 0.0, AltitudeMSL = 1500 },
                new() { Time = startTime.AddSeconds(135), VelocityDown = 6.0, VelocityNorth = 9.0, VelocityEast = 1.0, AltitudeMSL = 850 },
                new() { Time = startTime.AddSeconds(235), VelocityDown = 4.0, VelocityNorth = 7.0, VelocityEast = 0.0, AltitudeMSL = 200 }
            }
        };
        
        var landingSegment = new JumpSegment
        {
            Type = SegmentType.Landing,
            StartTime = startTime.AddSeconds(235),
            EndTime = startTime.AddSeconds(250),
            StartAltitude = 200,
            EndAltitude = 193,
            DataPoints = new List<DataPoint>
            {
                new() { Time = startTime.AddSeconds(235), VelocityDown = 3.0, VelocityNorth = 5.0, VelocityEast = 0.0, AltitudeMSL = 200 },
                new() { Time = startTime.AddSeconds(242), VelocityDown = 2.0, VelocityNorth = 4.0, VelocityEast = 0.0, AltitudeMSL = 196 },
                new() { Time = startTime.AddSeconds(250), VelocityDown = 1.0, VelocityNorth = 2.0, VelocityEast = 0.0, AltitudeMSL = 193 }
            }
        };

        var result = calculator.Calculate([freefallSegment, canopySegment, landingSegment]);

        Assert.NotNull(result.Freefall);
        Assert.NotNull(result.Canopy);
        Assert.NotNull(result.Landing);
        
        Assert.Equal(30.0, result.Freefall.TimeInFreefall);
        Assert.Equal(55.0, result.Freefall.MaxVerticalSpeed);
        
        Assert.Equal(1500, result.Canopy.DeploymentAltitude);
        Assert.Equal(200.0, result.Canopy.TotalCanopyTime);
        
        Assert.True(result.Landing.FinalApproachSpeed > 0);
    }

    [Fact]
    public void Calculate_MissingFreefallSegment_ReturnsNullForFreefall()
    {
        var calculator = new MetricsCalculator();
        var startTime = DateTime.UtcNow;
        
        // Only canopy segment (hop-n-pop scenario)
        var canopySegment = new JumpSegment
        {
            Type = SegmentType.Canopy,
            StartTime = startTime,
            EndTime = startTime.AddSeconds(100),
            StartAltitude = 1000,
            EndAltitude = 200,
            DataPoints = new List<DataPoint>
            {
                new() { Time = startTime, VelocityDown = 5.0, VelocityNorth = 8.0, VelocityEast = 0.0, AltitudeMSL = 1000 }
            }
        };

        var result = calculator.Calculate([canopySegment]);

        Assert.Null(result.Freefall);
        Assert.NotNull(result.Canopy);
        Assert.Null(result.Landing);
    }

    [Fact]
    public void Calculate_ShortSegment_StillCalculatesMetrics()
    {
        var calculator = new MetricsCalculator();
        var startTime = DateTime.UtcNow;
        
        // Segment with only 2 data points
        var segment = new JumpSegment
        {
            Type = SegmentType.Freefall,
            StartTime = startTime,
            EndTime = startTime.AddSeconds(1),
            StartAltitude = 2000,
            EndAltitude = 1990,
            DataPoints = new List<DataPoint>
            {
                new() { Time = startTime, VelocityDown = 20.0, VelocityNorth = 5.0, VelocityEast = 3.0, AltitudeMSL = 2000 },
                new() { Time = startTime.AddSeconds(1), VelocityDown = 25.0, VelocityNorth = 6.0, VelocityEast = 4.0, AltitudeMSL = 1990 }
            }
        };

        var result = calculator.Calculate([segment]);

        Assert.NotNull(result.Freefall);
        Assert.Equal(22.5, result.Freefall.AverageVerticalSpeed); // (20+25)/2
        Assert.Equal(25.0, result.Freefall.MaxVerticalSpeed);
        Assert.Equal(1.0, result.Freefall.TimeInFreefall);
    }

    [Fact]
    public void Calculate_SegmentWithNoDataPoints_ReturnsNull()
    {
        var calculator = new MetricsCalculator();
        
        var segment = new JumpSegment
        {
            Type = SegmentType.Freefall,
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddSeconds(10),
            StartAltitude = 2000,
            EndAltitude = 1900,
            DataPoints = [] // Empty list
        };

        var result = calculator.Calculate([segment]);

        Assert.Null(result.Freefall);
    }

    [Fact]
    public void CalculateFreefall_VerifiesHorizontalSpeedCalculation()
    {
        var calculator = new MetricsCalculator();
        
        // DataPoint with VelocityNorth = 3.0, VelocityEast = 4.0
        // HorizontalSpeed should be sqrt(3^2 + 4^2) = 5.0
        var dataPoints = new List<DataPoint>
        {
            new() { Time = DateTime.UtcNow, VelocityDown = 30.0, VelocityNorth = 3.0, VelocityEast = 4.0, AltitudeMSL = 2000 },
            new() { Time = DateTime.UtcNow.AddSeconds(1), VelocityDown = 30.0, VelocityNorth = 3.0, VelocityEast = 4.0, AltitudeMSL = 1970 }
        };
        
        var segment = new JumpSegment
        {
            Type = SegmentType.Freefall,
            StartTime = dataPoints[0].Time,
            EndTime = dataPoints[^1].Time,
            StartAltitude = 2000,
            EndAltitude = 1970,
            DataPoints = dataPoints
        };

        var result = calculator.Calculate([segment]);

        Assert.NotNull(result.Freefall);
        Assert.Equal(5.0, result.Freefall.AverageHorizontalSpeed, precision: 10);
    }

    [Fact]
    public void CalculateCanopy_ZeroAltitudeLoss_ReturnsZeroGlideRatio()
    {
        var calculator = new MetricsCalculator();
        var startTime = DateTime.UtcNow;
        
        // Flat flight with no altitude loss
        var dataPoints = new List<DataPoint>
        {
            new() { Time = startTime, VelocityDown = 0.0, VelocityNorth = 10.0, VelocityEast = 0.0, AltitudeMSL = 1000 },
            new() { Time = startTime.AddSeconds(10), VelocityDown = 0.0, VelocityNorth = 10.0, VelocityEast = 0.0, AltitudeMSL = 1000 }
        };
        
        var segment = new JumpSegment
        {
            Type = SegmentType.Canopy,
            StartTime = dataPoints[0].Time,
            EndTime = dataPoints[^1].Time,
            StartAltitude = 1000,
            EndAltitude = 1000,
            DataPoints = dataPoints
        };

        var result = calculator.Calculate([segment]);

        Assert.NotNull(result.Canopy);
        Assert.Equal(0.0, result.Canopy.GlideRatio);
    }

    [Fact]
    public void CalculateLanding_ShortSegment_UsesAllDataForApproach()
    {
        var calculator = new MetricsCalculator();
        var startTime = DateTime.UtcNow;
        
        // Landing segment shorter than 10 seconds
        var dataPoints = new List<DataPoint>
        {
            new() { Time = startTime, VelocityDown = 2.0, VelocityNorth = 6.0, VelocityEast = 0.0, AltitudeMSL = 200 },
            new() { Time = startTime.AddSeconds(3), VelocityDown = 1.5, VelocityNorth = 5.0, VelocityEast = 0.0, AltitudeMSL = 197 },
            new() { Time = startTime.AddSeconds(6), VelocityDown = 1.0, VelocityNorth = 4.0, VelocityEast = 0.0, AltitudeMSL = 195 }
        };
        
        var segment = new JumpSegment
        {
            Type = SegmentType.Landing,
            StartTime = dataPoints[0].Time,
            EndTime = dataPoints[^1].Time,
            StartAltitude = 200,
            EndAltitude = 195,
            DataPoints = dataPoints
        };

        var result = calculator.Calculate([segment]);

        Assert.NotNull(result.Landing);
        // Should use all data points since segment is shorter than 10 seconds
        Assert.True(result.Landing.FinalApproachSpeed > 4.0 && result.Landing.FinalApproachSpeed < 6.0);
    }
}
