using Azure;
using Azure.AI.OpenAI;
using JumpMetrics.Core.Configuration;
using JumpMetrics.Core.Interfaces;
using JumpMetrics.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using System.Text;
using System.Text.Json;

namespace JumpMetrics.Core.Services.AI;

public class AIAnalysisService : IAIAnalysisService
{
    private readonly AzureOpenAIClient _client;
    private readonly AzureOpenAIOptions _options;
    private readonly ILogger<AIAnalysisService> _logger;

    public AIAnalysisService(
        IOptions<AzureOpenAIOptions> options,
        ILogger<AIAnalysisService> logger)
    {
        _options = options.Value;
        _logger = logger;

        if (string.IsNullOrEmpty(_options.Endpoint) || string.IsNullOrEmpty(_options.ApiKey))
        {
            throw new ArgumentException("Azure OpenAI endpoint and API key must be configured.");
        }

        _client = new AzureOpenAIClient(
            new Uri(_options.Endpoint),
            new AzureKeyCredential(_options.ApiKey));
    }

    public async Task<AIAnalysis> AnalyzeAsync(Jump jump, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(jump);

        _logger.LogInformation("Starting AI analysis for jump {JumpId}", jump.JumpId);

        try
        {
            // Build the user prompt with jump data
            var userPrompt = BuildUserPrompt(jump);

            // Get the chat client
            var chatClient = _client.GetChatClient(_options.DeploymentName);

            // Create chat messages
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(GetSystemPrompt()),
                new UserChatMessage(userPrompt)
            };

            // Create chat completion options
            var chatOptions = new ChatCompletionOptions
            {
                MaxOutputTokenCount = _options.MaxTokens,
                Temperature = (float)_options.Temperature,
                ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
            };

            // Call Azure OpenAI
            var response = await chatClient.CompleteChatAsync(messages, chatOptions, cancellationToken);

            // Parse the response
            var analysisJson = response.Value.Content[0].Text;
            var analysis = ParseAnalysisResponse(analysisJson);

            _logger.LogInformation("AI analysis completed successfully for jump {JumpId}", jump.JumpId);

            return analysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during AI analysis for jump {JumpId}", jump.JumpId);
            throw;
        }
    }

    private string GetSystemPrompt()
    {
        return @"You are an expert skydiving safety analyst and coach with extensive knowledge of skydiving operations, 
safety standards, and performance optimization. You analyze FlySight GPS data from skydives to provide actionable 
insights, identify safety concerns, and offer progression recommendations.

Your expertise includes:
- USPA license levels (A, B, C, D) and associated privileges and limitations
- Skydiving safety standards and best practices
- Freefall performance analysis (tracking, fall rates, stability)
- Canopy flight performance (descent rates, glide ratios, landing patterns)
- Landing safety and accuracy
- Equipment suitability assessment
- Progression planning for skill development

When analyzing jumps, you MUST:
1. Be safety-focused and conservative in your recommendations
2. Identify potential safety concerns with appropriate severity levels (Info, Warning, Critical)
3. Provide specific, actionable feedback based on the actual data
4. Consider the skydiver's experience level when making recommendations
5. Recognize common patterns that indicate skill areas needing improvement
6. Offer constructive guidance for progression

Safety flags to watch for:
- LOW PULL: Deployment below 2,500 feet AGL (Warning) or below 2,000 feet AGL (Critical)
- AGGRESSIVE CANOPY: Maximum horizontal speed > 20 m/s during canopy flight (Warning) or > 25 m/s (Critical)
- HARD LANDING: Touchdown vertical speed > 3 m/s (Warning) or > 5 m/s (Critical)
- POOR PATTERN: Pattern altitude below 300m AGL (Warning) or pattern entry issues
- LONG SPOT: Landing more than 500m from dropzone (Warning)
- UNSTABLE FREEFALL: Excessive variance in vertical speed (> 5 m/s standard deviation)

You must respond with valid JSON in the following structure:
{
  ""overallAssessment"": ""Overall performance summary"",
  ""safetyFlags"": [
    {
      ""category"": ""LOW_PULL|AGGRESSIVE_CANOPY|HARD_LANDING|POOR_PATTERN|LONG_SPOT|UNSTABLE_FREEFALL"",
      ""description"": ""Detailed description of the safety concern"",
      ""severity"": ""Info|Warning|Critical""
    }
  ],
  ""strengths"": [""Specific strength 1"", ""Specific strength 2""],
  ""improvementAreas"": [""Specific improvement area 1"", ""Specific improvement area 2""],
  ""progressionRecommendation"": ""Detailed recommendation for next steps"",
  ""skillLevel"": 5
}

Skill level is rated 1-10 based on demonstrated proficiency in freefall control, canopy flight, and landing accuracy.";
    }

    private string BuildUserPrompt(Jump jump)
    {
        var sb = new StringBuilder();

        sb.AppendLine("Please analyze this skydive and provide detailed feedback:");
        sb.AppendLine();
        sb.AppendLine($"Jump ID: {jump.JumpId}");
        sb.AppendLine($"Date: {jump.JumpDate:yyyy-MM-dd}");
        sb.AppendLine($"File: {jump.FlySightFileName}");
        sb.AppendLine();

        // Add metadata
        if (jump.Metadata != null)
        {
            sb.AppendLine("Recording Metadata:");
            sb.AppendLine($"- Recording Duration: {(jump.Metadata.RecordingEnd - jump.Metadata.RecordingStart)?.TotalMinutes:F1} minutes");
            sb.AppendLine($"- Data Points: {jump.Metadata.TotalDataPoints}");
            sb.AppendLine($"- Max Altitude: {jump.Metadata.MaxAltitude:F0}m MSL");
            sb.AppendLine($"- Min Altitude: {jump.Metadata.MinAltitude:F0}m MSL");
            sb.AppendLine();
        }

        // Add segment information
        if (jump.Segments != null && jump.Segments.Count > 0)
        {
            sb.AppendLine("Jump Segments:");
            foreach (var segment in jump.Segments)
            {
                sb.AppendLine($"- {segment.Type}: {segment.Duration:F1}s, {segment.StartAltitude:F0}m -> {segment.EndAltitude:F0}m");
            }
            sb.AppendLine();
        }

        // Add performance metrics
        if (jump.Metrics != null)
        {
            if (jump.Metrics.Freefall != null)
            {
                sb.AppendLine("Freefall Metrics:");
                sb.AppendLine($"- Time in Freefall: {jump.Metrics.Freefall.TimeInFreefall:F1}s");
                sb.AppendLine($"- Average Vertical Speed: {jump.Metrics.Freefall.AverageVerticalSpeed:F1} m/s ({jump.Metrics.Freefall.AverageVerticalSpeed * 2.237:F0} mph)");
                sb.AppendLine($"- Max Vertical Speed: {jump.Metrics.Freefall.MaxVerticalSpeed:F1} m/s ({jump.Metrics.Freefall.MaxVerticalSpeed * 2.237:F0} mph)");
                sb.AppendLine($"- Average Horizontal Speed: {jump.Metrics.Freefall.AverageHorizontalSpeed:F1} m/s");
                sb.AppendLine($"- Track Angle: {jump.Metrics.Freefall.TrackAngle:F0}°");
                sb.AppendLine();
            }

            if (jump.Metrics.Canopy != null)
            {
                sb.AppendLine("Canopy Flight Metrics:");
                sb.AppendLine($"- Deployment Altitude: {jump.Metrics.Canopy.DeploymentAltitude:F0}m MSL");
                
                // Calculate AGL (assuming ground level is near MinAltitude)
                var groundLevel = jump.Metadata?.MinAltitude ?? 0;
                var deploymentAGL = jump.Metrics.Canopy.DeploymentAltitude - groundLevel;
                sb.AppendLine($"- Deployment Altitude AGL: ~{deploymentAGL:F0}m (~{deploymentAGL * 3.281:F0} feet)");
                
                sb.AppendLine($"- Total Canopy Time: {jump.Metrics.Canopy.TotalCanopyTime:F1}s");
                sb.AppendLine($"- Average Descent Rate: {jump.Metrics.Canopy.AverageDescentRate:F1} m/s");
                sb.AppendLine($"- Glide Ratio: {jump.Metrics.Canopy.GlideRatio:F2}:1");
                sb.AppendLine($"- Max Horizontal Speed: {jump.Metrics.Canopy.MaxHorizontalSpeed:F1} m/s ({jump.Metrics.Canopy.MaxHorizontalSpeed * 2.237:F0} mph)");
                
                if (jump.Metrics.Canopy.PatternAltitude.HasValue)
                {
                    var patternAGL = jump.Metrics.Canopy.PatternAltitude.Value - groundLevel;
                    sb.AppendLine($"- Pattern Altitude: {jump.Metrics.Canopy.PatternAltitude:F0}m MSL (~{patternAGL:F0}m AGL)");
                }
                sb.AppendLine();
            }

            if (jump.Metrics.Landing != null)
            {
                sb.AppendLine("Landing Metrics:");
                sb.AppendLine($"- Final Approach Speed: {jump.Metrics.Landing.FinalApproachSpeed:F1} m/s ({jump.Metrics.Landing.FinalApproachSpeed * 2.237:F0} mph)");
                sb.AppendLine($"- Touchdown Vertical Speed: {jump.Metrics.Landing.TouchdownVerticalSpeed:F1} m/s");
                
                if (jump.Metrics.Landing.LandingAccuracy.HasValue)
                {
                    sb.AppendLine($"- Landing Accuracy: {jump.Metrics.Landing.LandingAccuracy:F0}m from target");
                }
                sb.AppendLine();
            }
        }

        sb.AppendLine("Note: This appears to be a hop-n-pop (short delay) jump based on the altitude and freefall time.");

        return sb.ToString();
    }

    private AIAnalysis ParseAnalysisResponse(string json)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var result = JsonSerializer.Deserialize<AIAnalysisResponse>(json, options);

            if (result == null)
            {
                throw new InvalidOperationException("Failed to deserialize AI analysis response");
            }

            return new AIAnalysis
            {
                OverallAssessment = result.OverallAssessment ?? string.Empty,
                SafetyFlags = result.SafetyFlags?.Select(f => new SafetyFlag
                {
                    Category = f.Category ?? "UNKNOWN",
                    Description = f.Description ?? string.Empty,
                    Severity = ParseSeverity(f.Severity)
                }).ToList() ?? [],
                Strengths = result.Strengths ?? [],
                ImprovementAreas = result.ImprovementAreas ?? [],
                ProgressionRecommendation = result.ProgressionRecommendation ?? string.Empty,
                SkillLevel = result.SkillLevel
            };
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse AI analysis response JSON: {Json}", json);
            throw new InvalidOperationException("Failed to parse AI analysis response", ex);
        }
    }

    private SafetySeverity ParseSeverity(string? severity)
    {
        return severity?.ToUpperInvariant() switch
        {
            "CRITICAL" => SafetySeverity.Critical,
            "WARNING" => SafetySeverity.Warning,
            "INFO" => SafetySeverity.Info,
            _ => SafetySeverity.Info
        };
    }

    // Internal class for JSON deserialization
    private class AIAnalysisResponse
    {
        public string? OverallAssessment { get; set; }
        public List<SafetyFlagResponse>? SafetyFlags { get; set; }
        public List<string>? Strengths { get; set; }
        public List<string>? ImprovementAreas { get; set; }
        public string? ProgressionRecommendation { get; set; }
        public int SkillLevel { get; set; }
    }

    private class SafetyFlagResponse
    {
        public string? Category { get; set; }
        public string? Description { get; set; }
        public string? Severity { get; set; }
    }
}
