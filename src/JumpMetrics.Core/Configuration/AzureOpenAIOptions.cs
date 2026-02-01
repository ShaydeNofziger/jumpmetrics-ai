namespace JumpMetrics.Core.Configuration;

public class AzureOpenAIOptions
{
    public const string SectionName = "AzureOpenAI";

    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string DeploymentName { get; set; } = "gpt-4";
    public int MaxTokens { get; set; } = 2000;
    public double Temperature { get; set; } = 0.7;
}
