namespace JumpMetrics.Core.Models;

public class AIAnalysis
{
    public string OverallAssessment { get; set; } = string.Empty;
    public List<SafetyFlag> SafetyFlags { get; set; } = [];
    public List<string> Strengths { get; set; } = [];
    public List<string> ImprovementAreas { get; set; } = [];
    public string ProgressionRecommendation { get; set; } = string.Empty;
    public int SkillLevel { get; set; }
}

public class SafetyFlag
{
    public required string Category { get; set; }
    public required string Description { get; set; }
    public SafetySeverity Severity { get; set; }
}

public enum SafetySeverity
{
    Info,
    Warning,
    Critical
}
