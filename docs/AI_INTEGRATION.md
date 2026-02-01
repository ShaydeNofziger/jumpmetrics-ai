# Azure OpenAI Integration

## Overview

The Azure OpenAI integration provides AI-powered analysis of skydiving jump data using GPT-4. The `AIAnalysisService` generates safety-focused insights, identifies potential concerns, and offers progression recommendations based on FlySight GPS data.

## Configuration

### appsettings.json

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://your-openai-endpoint.openai.azure.com/",
    "ApiKey": "your-api-key-here",
    "DeploymentName": "gpt-4",
    "MaxTokens": 2000,
    "Temperature": 0.7
  }
}
```

### Environment Variables

For production deployments, use environment variables instead of storing credentials in config files:

```bash
export AzureOpenAI__Endpoint="https://your-openai-endpoint.openai.azure.com/"
export AzureOpenAI__ApiKey="your-api-key-here"
```

## System Prompt

The AI agent is configured with expert skydiving knowledge including:

- **USPA License Levels**: Understanding of A, B, C, D license privileges
- **Safety Standards**: Best practices and regulatory requirements
- **Performance Analysis**: Freefall dynamics, canopy flight, landing techniques
- **Equipment Assessment**: Suitability recommendations based on skill level
- **Progression Planning**: Structured advancement guidance

## Safety Flag Detection

The AI automatically identifies and categorizes safety concerns:

### Critical Flags

- **LOW PULL**: Deployment below 2,000 feet AGL
- **AGGRESSIVE CANOPY**: Maximum horizontal speed > 25 m/s
- **HARD LANDING**: Touchdown vertical speed > 5 m/s

### Warning Flags

- **LOW PULL**: Deployment below 2,500 feet AGL (but above 2,000)
- **AGGRESSIVE CANOPY**: Maximum horizontal speed > 20 m/s (but below 25)
- **HARD LANDING**: Touchdown vertical speed > 3 m/s (but below 5)
- **POOR PATTERN**: Pattern altitude below 300m AGL
- **LONG SPOT**: Landing more than 500m from dropzone
- **UNSTABLE FREEFALL**: Excessive variance in vertical speed

### Info Flags

- General observations and educational points
- Suggestions for skill improvement
- Equipment compatibility notes

## Response Structure

The AI returns structured JSON that maps to the `AIAnalysis` model:

```csharp
public class AIAnalysis
{
    public string OverallAssessment { get; set; }
    public List<SafetyFlag> SafetyFlags { get; set; }
    public List<string> Strengths { get; set; }
    public List<string> ImprovementAreas { get; set; }
    public string ProgressionRecommendation { get; set; }
    public int SkillLevel { get; set; } // 1-10 rating
}
```

## Usage Example

```csharp
// Inject the service
public class MyFunction
{
    private readonly IAIAnalysisService _aiService;

    public MyFunction(IAIAnalysisService aiService)
    {
        _aiService = aiService;
    }

    public async Task ProcessJump(Jump jump)
    {
        var analysis = await _aiService.AnalyzeAsync(jump);

        Console.WriteLine($"Overall: {analysis.OverallAssessment}");
        Console.WriteLine($"Skill Level: {analysis.SkillLevel}/10");

        foreach (var flag in analysis.SafetyFlags)
        {
            Console.WriteLine($"[{flag.Severity}] {flag.Category}: {flag.Description}");
        }

        Console.WriteLine($"\nStrengths:");
        foreach (var strength in analysis.Strengths)
        {
            Console.WriteLine($"  - {strength}");
        }

        Console.WriteLine($"\nAreas for Improvement:");
        foreach (var area in analysis.ImprovementAreas)
        {
            Console.WriteLine($"  - {area}");
        }

        Console.WriteLine($"\nNext Steps: {analysis.ProgressionRecommendation}");
    }
}
```

## Performance Considerations

- **Response Time**: Target < 10 seconds for typical jumps
- **Token Usage**: Configured for ~2000 max output tokens (adjustable)
- **Temperature**: Set to 0.7 for balanced creativity and consistency
- **Retry Logic**: Implement exponential backoff for transient failures
- **Rate Limiting**: Monitor Azure OpenAI quota and implement throttling

## Best Practices

1. **Always validate configuration** before deployment (non-empty endpoint and key)
2. **Use structured output** (JSON format) for consistent parsing
3. **Log AI requests and responses** for debugging and improvement
4. **Handle errors gracefully** - provide fallback messaging if AI fails
5. **Monitor costs** - Azure OpenAI charges per token
6. **Test with diverse jump types** - ensure prompts work for various scenarios

## Testing

Run the unit tests:

```bash
dotnet test tests/JumpMetrics.Core.Tests --filter "FullyQualifiedName~AIAnalysisServiceTests"
```

The test suite includes:
- Configuration validation
- Null input handling
- Service construction
- Sample jump data creation for integration testing

## Troubleshooting

### Common Issues

**Error: "Azure OpenAI endpoint and API key must be configured"**
- Solution: Ensure `AzureOpenAI:Endpoint` and `AzureOpenAI:ApiKey` are set in appsettings or environment variables

**Error: 401 Unauthorized**
- Solution: Verify API key is correct and has not expired

**Error: 429 Too Many Requests**
- Solution: Implement rate limiting or increase Azure OpenAI quota

**Error: Timeout**
- Solution: Increase timeout value or reduce complexity of jump data being analyzed

## Security Notes

- **Never commit API keys** to source control
- Use **Azure Key Vault** for production credential storage
- Implement **managed identity** authentication when possible
- Rotate API keys regularly
- Monitor for unusual usage patterns

## Future Enhancements

- [ ] Batch analysis for multiple jumps
- [ ] Comparison analysis (jump-to-jump progression tracking)
- [ ] Equipment-specific recommendations
- [ ] Weather correlation analysis
- [ ] Formation skydiving pattern recognition
- [ ] Video overlay generation with AI insights
