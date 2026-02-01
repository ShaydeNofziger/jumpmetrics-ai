# AI Analysis Service Usage Examples

This document provides code examples demonstrating how to use the AIAnalysisService in various scenarios.

## Basic Usage

### 1. Service Registration (Dependency Injection)

```csharp
using JumpMetrics.Core.Configuration;
using JumpMetrics.Core.Interfaces;
using JumpMetrics.Core.Services.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

// In your Program.cs or Startup.cs
services.Configure<AzureOpenAIOptions>(
    configuration.GetSection(AzureOpenAIOptions.SectionName));

services.AddScoped<IAIAnalysisService, AIAnalysisService>();
```

### 2. Analyzing a Jump

```csharp
using JumpMetrics.Core.Interfaces;
using JumpMetrics.Core.Models;

public class JumpProcessor
{
    private readonly IAIAnalysisService _aiService;

    public JumpProcessor(IAIAnalysisService aiService)
    {
        _aiService = aiService;
    }

    public async Task<AIAnalysis> ProcessJumpAsync(Jump jump)
    {
        // Analyze the jump using AI
        var analysis = await _aiService.AnalyzeAsync(jump);

        // Access the results
        Console.WriteLine($"Overall Assessment: {analysis.OverallAssessment}");
        Console.WriteLine($"Skill Level: {analysis.SkillLevel}/10");

        // Check for safety flags
        foreach (var flag in analysis.SafetyFlags)
        {
            var emoji = flag.Severity switch
            {
                SafetySeverity.Critical => "🚨",
                SafetySeverity.Warning => "⚠️",
                SafetySeverity.Info => "ℹ️",
                _ => "•"
            };

            Console.WriteLine($"{emoji} [{flag.Severity}] {flag.Category}");
            Console.WriteLine($"   {flag.Description}");
        }

        return analysis;
    }
}
```

### 3. Creating a Jump from Parsed Data

```csharp
using JumpMetrics.Core.Models;

var jump = new Jump
{
    JumpId = Guid.NewGuid(),
    JumpDate = DateTime.UtcNow,
    FlySightFileName = "my-jump.csv",
    
    Metadata = new JumpMetadata
    {
        TotalDataPoints = 1972,
        RecordingStart = DateTime.Parse("2025-09-11T17:26:18Z"),
        RecordingEnd = DateTime.Parse("2025-09-11T17:32:48Z"),
        MaxAltitude = 1910, // meters MSL
        MinAltitude = 193   // meters MSL
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
            TouchdownVerticalSpeed = 2.5
        }
    }
};

// Analyze the jump
var analysis = await aiService.AnalyzeAsync(jump);
```

## Advanced Scenarios

### 4. Error Handling

```csharp
public async Task<AIAnalysis?> SafeAnalyzeAsync(Jump jump)
{
    try
    {
        return await _aiService.AnalyzeAsync(jump);
    }
    catch (ArgumentException ex)
    {
        Console.WriteLine($"Configuration error: {ex.Message}");
        return null;
    }
    catch (InvalidOperationException ex)
    {
        Console.WriteLine($"Parsing error: {ex.Message}");
        return null;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Unexpected error during AI analysis: {ex.Message}");
        return null;
    }
}
```

### 5. Filtering Safety Flags by Severity

```csharp
public void DisplayCriticalIssues(AIAnalysis analysis)
{
    var criticalFlags = analysis.SafetyFlags
        .Where(f => f.Severity == SafetySeverity.Critical)
        .ToList();

    if (criticalFlags.Any())
    {
        Console.WriteLine("⚠️  CRITICAL SAFETY CONCERNS:");
        foreach (var flag in criticalFlags)
        {
            Console.WriteLine($"  • {flag.Category}: {flag.Description}");
        }
    }
}
```

### 6. Generating a Text Report

```csharp
public string GenerateReport(Jump jump, AIAnalysis analysis)
{
    var sb = new StringBuilder();
    
    sb.AppendLine($"Jump Analysis Report");
    sb.AppendLine($"===================");
    sb.AppendLine();
    sb.AppendLine($"Jump ID: {jump.JumpId}");
    sb.AppendLine($"Date: {jump.JumpDate:yyyy-MM-dd HH:mm:ss}");
    sb.AppendLine($"File: {jump.FlySightFileName}");
    sb.AppendLine();
    
    sb.AppendLine($"Overall Assessment:");
    sb.AppendLine($"{analysis.OverallAssessment}");
    sb.AppendLine();
    
    sb.AppendLine($"Skill Level: {analysis.SkillLevel}/10");
    sb.AppendLine();
    
    if (analysis.SafetyFlags.Any())
    {
        sb.AppendLine("Safety Flags:");
        foreach (var flag in analysis.SafetyFlags.OrderByDescending(f => f.Severity))
        {
            sb.AppendLine($"  [{flag.Severity}] {flag.Category}");
            sb.AppendLine($"    {flag.Description}");
        }
        sb.AppendLine();
    }
    
    if (analysis.Strengths.Any())
    {
        sb.AppendLine("Strengths:");
        foreach (var strength in analysis.Strengths)
        {
            sb.AppendLine($"  ✓ {strength}");
        }
        sb.AppendLine();
    }
    
    if (analysis.ImprovementAreas.Any())
    {
        sb.AppendLine("Areas for Improvement:");
        foreach (var area in analysis.ImprovementAreas)
        {
            sb.AppendLine($"  → {area}");
        }
        sb.AppendLine();
    }
    
    sb.AppendLine("Progression Recommendation:");
    sb.AppendLine($"{analysis.ProgressionRecommendation}");
    
    return sb.ToString();
}
```

### 7. Batch Processing Multiple Jumps

```csharp
public async Task<Dictionary<Guid, AIAnalysis>> AnalyzeMultipleJumpsAsync(
    List<Jump> jumps,
    IProgress<int>? progress = null)
{
    var results = new Dictionary<Guid, AIAnalysis>();
    var processed = 0;

    foreach (var jump in jumps)
    {
        try
        {
            var analysis = await _aiService.AnalyzeAsync(jump);
            results[jump.JumpId] = analysis;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to analyze jump {jump.JumpId}: {ex.Message}");
        }

        processed++;
        progress?.Report((int)((processed / (double)jumps.Count) * 100));
    }

    return results;
}
```

### 8. Using with Azure Functions

```csharp
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

public class AnalyzeJumpFunction
{
    private readonly IAIAnalysisService _aiService;

    public AnalyzeJumpFunction(IAIAnalysisService aiService)
    {
        _aiService = aiService;
    }

    [Function("AnalyzeJump")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        // Parse the jump from the request body
        var jump = await req.ReadFromJsonAsync<Jump>();
        
        if (jump == null)
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteStringAsync("Invalid jump data");
            return badResponse;
        }

        // Analyze the jump
        var analysis = await _aiService.AnalyzeAsync(jump);

        // Return the analysis
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(analysis);
        return response;
    }
}
```

## Configuration Examples

### appsettings.json

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://your-instance.openai.azure.com/",
    "ApiKey": "your-api-key-here",
    "DeploymentName": "gpt-4",
    "MaxTokens": 2000,
    "Temperature": 0.7
  }
}
```

### Environment Variables (Production)

```bash
# Linux/macOS
export AzureOpenAI__Endpoint="https://your-instance.openai.azure.com/"
export AzureOpenAI__ApiKey="your-api-key"
export AzureOpenAI__DeploymentName="gpt-4"

# Windows (PowerShell)
$env:AzureOpenAI__Endpoint="https://your-instance.openai.azure.com/"
$env:AzureOpenAI__ApiKey="your-api-key"
$env:AzureOpenAI__DeploymentName="gpt-4"

# Windows (CMD)
set AzureOpenAI__Endpoint=https://your-instance.openai.azure.com/
set AzureOpenAI__ApiKey=your-api-key
set AzureOpenAI__DeploymentName=gpt-4
```

## Testing

### Unit Test Example

```csharp
using JumpMetrics.Core.Configuration;
using JumpMetrics.Core.Services.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

public class AIAnalysisServiceTests
{
    [Fact]
    public void Constructor_WithValidOptions_Succeeds()
    {
        var options = Options.Create(new AzureOpenAIOptions
        {
            Endpoint = "https://test.openai.azure.com",
            ApiKey = "test-key"
        });
        var logger = Mock.Of<ILogger<AIAnalysisService>>();

        var service = new AIAnalysisService(options, logger);

        Assert.NotNull(service);
    }
}
```

## Best Practices

1. **Always use dependency injection** for service registration
2. **Handle exceptions gracefully** - AI services can fail due to network issues, rate limits, etc.
3. **Validate configuration** before calling the service
4. **Log AI requests and responses** for debugging and monitoring
5. **Monitor costs** - Azure OpenAI charges per token
6. **Implement retry logic** with exponential backoff for transient failures
7. **Use structured logging** to track analysis patterns and identify issues

## Troubleshooting

### Common Issues

1. **"Azure OpenAI endpoint and API key must be configured"**
   - Ensure configuration is properly loaded
   - Check environment variables are set correctly

2. **401 Unauthorized**
   - Verify API key is correct
   - Check key hasn't expired

3. **429 Too Many Requests**
   - Implement rate limiting
   - Add retry logic with exponential backoff

4. **JSON Parsing Errors**
   - AI response format may vary
   - Check logs for actual response content
   - Ensure deployment is GPT-4 or compatible model

For more information, see [AI_INTEGRATION.md](AI_INTEGRATION.md).
