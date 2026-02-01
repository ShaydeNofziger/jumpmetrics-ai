using JumpMetrics.Core.Configuration;
using JumpMetrics.Core.Models;
using JumpMetrics.Core.Services.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace JumpMetrics.Core.Tests.Integration;

/// <summary>
/// Integration test examples showing how to use AIAnalysisService.
/// These tests require actual Azure OpenAI credentials to run.
/// To run these tests, configure your Azure OpenAI credentials in user secrets or environment variables.
/// </summary>
public class AIAnalysisServiceIntegrationTests
{
    /// <summary>
    /// Example test showing how to analyze a hop-n-pop jump.
    /// This test is skipped by default as it requires Azure OpenAI credentials.
    /// To enable: Remove the Skip attribute and configure your Azure OpenAI credentials.
    /// </summary>
    [Fact(Skip = "Requires Azure OpenAI credentials")]
    public async Task AnalyzeAsync_HopNPopJump_ReturnsValidAnalysis()
    {
        // Arrange - Configure with your Azure OpenAI credentials
        var options = Options.Create(new AzureOpenAIOptions
        {
            Endpoint = "https://your-openai-endpoint.openai.azure.com/",
            ApiKey = "your-api-key-here",
            DeploymentName = "gpt-4",
            MaxTokens = 2000,
            Temperature = 0.7
        });

        var logger = new Mock<ILogger<AIAnalysisService>>();
        var service = new AIAnalysisService(options, logger.Object);

        // Create a realistic hop-n-pop jump scenario
        var jump = CreateHopNPopJump();

        // Act
        var analysis = await service.AnalyzeAsync(jump);

        // Assert
        Assert.NotNull(analysis);
        Assert.NotEmpty(analysis.OverallAssessment);
        Assert.InRange(analysis.SkillLevel, 1, 10);
        
        // Should detect low pull (deployment at 1,780m with ~193m ground = ~1,587m AGL = ~5,207 feet AGL)
        // This is above 2,500 feet so should be OK
        
        Assert.NotNull(analysis.SafetyFlags);
        Assert.NotNull(analysis.Strengths);
        Assert.NotNull(analysis.ImprovementAreas);
        Assert.NotEmpty(analysis.ProgressionRecommendation);

        // Log the results
        Console.WriteLine("=== AI Analysis Results ===");
        Console.WriteLine($"Overall Assessment: {analysis.OverallAssessment}");
        Console.WriteLine($"Skill Level: {analysis.SkillLevel}/10");
        Console.WriteLine();

        if (analysis.SafetyFlags.Count > 0)
        {
            Console.WriteLine("Safety Flags:");
            foreach (var flag in analysis.SafetyFlags)
            {
                Console.WriteLine($"  [{flag.Severity}] {flag.Category}: {flag.Description}");
            }
            Console.WriteLine();
        }

        if (analysis.Strengths.Count > 0)
        {
            Console.WriteLine("Strengths:");
            foreach (var strength in analysis.Strengths)
            {
                Console.WriteLine($"  - {strength}");
            }
            Console.WriteLine();
        }

        if (analysis.ImprovementAreas.Count > 0)
        {
            Console.WriteLine("Areas for Improvement:");
            foreach (var area in analysis.ImprovementAreas)
            {
                Console.WriteLine($"  - {area}");
            }
            Console.WriteLine();
        }

        Console.WriteLine($"Progression Recommendation: {analysis.ProgressionRecommendation}");
    }

    /// <summary>
    /// Example test showing analysis of a jump with safety concerns.
    /// </summary>
    [Fact(Skip = "Requires Azure OpenAI credentials")]
    public async Task AnalyzeAsync_LowPullJump_IdentifiesSafetyConcerns()
    {
        // Arrange
        var options = Options.Create(new AzureOpenAIOptions
        {
            Endpoint = "https://your-openai-endpoint.openai.azure.com/",
            ApiKey = "your-api-key-here",
            DeploymentName = "gpt-4"
        });

        var logger = new Mock<ILogger<AIAnalysisService>>();
        var service = new AIAnalysisService(options, logger.Object);

        // Create a jump with a low pull (deployment at 800m AGL = ~2,625 feet)
        var jump = CreateLowPullJump();

        // Act
        var analysis = await service.AnalyzeAsync(jump);

        // Assert
        Assert.NotNull(analysis);
        Assert.NotEmpty(analysis.SafetyFlags);
        
        // Should detect low pull warning (below 2,500 feet AGL but above 2,000)
        Assert.Contains(analysis.SafetyFlags, f => 
            f.Category.Contains("LOW_PULL", StringComparison.OrdinalIgnoreCase));

        var lowPullFlag = analysis.SafetyFlags.First(f => 
            f.Category.Contains("LOW_PULL", StringComparison.OrdinalIgnoreCase));
        
        Assert.Equal(SafetySeverity.Warning, lowPullFlag.Severity);
    }

    private Jump CreateHopNPopJump()
    {
        return new Jump
        {
            JumpId = Guid.NewGuid(),
            JumpDate = DateTime.UtcNow,
            FlySightFileName = "sample-jump.csv",
            Metadata = new JumpMetadata
            {
                TotalDataPoints = 1972,
                RecordingStart = DateTime.UtcNow.AddMinutes(-7),
                RecordingEnd = DateTime.UtcNow,
                MaxAltitude = 1910,
                MinAltitude = 193
            },
            Segments = new List<JumpSegment>
            {
                new JumpSegment
                {
                    Type = SegmentType.Freefall,
                    StartTime = DateTime.UtcNow.AddMinutes(-5),
                    EndTime = DateTime.UtcNow.AddMinutes(-4.75),
                    StartAltitude = 1910,
                    EndAltitude = 1780,
                    DataPoints = new List<DataPoint>()
                },
                new JumpSegment
                {
                    Type = SegmentType.Canopy,
                    StartTime = DateTime.UtcNow.AddMinutes(-4.75),
                    EndTime = DateTime.UtcNow.AddMinutes(-1),
                    StartAltitude = 1740,
                    EndAltitude = 193,
                    DataPoints = new List<DataPoint>()
                }
            },
            Metrics = new JumpPerformanceMetrics
            {
                Freefall = new FreefallMetrics
                {
                    TimeInFreefall = 15.0,
                    AverageVerticalSpeed = 25.0,
                    MaxVerticalSpeed = 27.0,
                    AverageHorizontalSpeed = 12.0,
                    TrackAngle = 45.0
                },
                Canopy = new CanopyMetrics
                {
                    DeploymentAltitude = 1780,
                    TotalCanopyTime = 240.0,
                    AverageDescentRate = 5.0,
                    GlideRatio = 3.5,
                    MaxHorizontalSpeed = 15.0,
                    PatternAltitude = 400
                },
                Landing = new LandingMetrics
                {
                    FinalApproachSpeed = 8.0,
                    TouchdownVerticalSpeed = 2.5,
                    LandingAccuracy = null
                }
            }
        };
    }

    private Jump CreateLowPullJump()
    {
        return new Jump
        {
            JumpId = Guid.NewGuid(),
            JumpDate = DateTime.UtcNow,
            FlySightFileName = "low-pull-jump.csv",
            Metadata = new JumpMetadata
            {
                TotalDataPoints = 1500,
                RecordingStart = DateTime.UtcNow.AddMinutes(-6),
                RecordingEnd = DateTime.UtcNow,
                MaxAltitude = 1910,
                MinAltitude = 193
            },
            Segments = new List<JumpSegment>
            {
                new JumpSegment
                {
                    Type = SegmentType.Freefall,
                    StartTime = DateTime.UtcNow.AddMinutes(-5),
                    EndTime = DateTime.UtcNow.AddMinutes(-4.5),
                    StartAltitude = 1910,
                    EndAltitude = 993, // Low pull: 800m AGL
                    DataPoints = new List<DataPoint>()
                },
                new JumpSegment
                {
                    Type = SegmentType.Canopy,
                    StartTime = DateTime.UtcNow.AddMinutes(-4.5),
                    EndTime = DateTime.UtcNow.AddMinutes(-1),
                    StartAltitude = 993,
                    EndAltitude = 193,
                    DataPoints = new List<DataPoint>()
                }
            },
            Metrics = new JumpPerformanceMetrics
            {
                Freefall = new FreefallMetrics
                {
                    TimeInFreefall = 30.0,
                    AverageVerticalSpeed = 50.0,
                    MaxVerticalSpeed = 55.0,
                    AverageHorizontalSpeed = 15.0,
                    TrackAngle = 30.0
                },
                Canopy = new CanopyMetrics
                {
                    DeploymentAltitude = 993, // ~800m AGL = ~2,625 feet AGL (WARNING)
                    TotalCanopyTime = 210.0,
                    AverageDescentRate = 3.8,
                    GlideRatio = 4.0,
                    MaxHorizontalSpeed = 12.0,
                    PatternAltitude = 350
                },
                Landing = new LandingMetrics
                {
                    FinalApproachSpeed = 7.5,
                    TouchdownVerticalSpeed = 2.0,
                    LandingAccuracy = null
                }
            }
        };
    }
}
