function Get-JumpAnalysis {
    <#
    .SYNOPSIS
        Retrieves AI-powered analysis for a jump.
    .DESCRIPTION
        Generates or retrieves AI-powered analysis including performance assessment,
        safety flags, and progression recommendations for a processed jump.
        Can accept either a JumpId (for Azure-stored jumps) or a jump object with metrics.
    .PARAMETER JumpId
        The unique identifier of the jump to analyze (requires Azure OpenAI).
    .PARAMETER Jump
        A jump object returned from Import-FlySightData.
    .PARAMETER Metrics
        Calculated metrics object from Get-JumpMetrics.
    .OUTPUTS
        PSCustomObject containing AI analysis, safety flags, and recommendations.
    .EXAMPLE
        $jump = Import-FlySightData -Path .\samples\sample-jump.csv
        $metrics = Get-JumpMetrics -Jump $jump
        Get-JumpAnalysis -Jump $jump -Metrics $metrics
        
        Generate AI analysis for a locally parsed jump (requires Azure OpenAI).
    .EXAMPLE
        Get-JumpAnalysis -JumpId 'a1b2c3d4-e5f6-7890-abcd-ef1234567890'
        
        Retrieve AI analysis for a jump stored in Azure.
    #>
    [CmdletBinding(DefaultParameterSetName = 'Jump')]
    param(
        [Parameter(Mandatory, ParameterSetName = 'JumpId')]
        [guid]$JumpId,

        [Parameter(Mandatory, ParameterSetName = 'Jump')]
        [PSCustomObject]$Jump,

        [Parameter(ParameterSetName = 'Jump')]
        [PSCustomObject]$Metrics
    )

    try {
        # Check for Azure OpenAI configuration
        $hasAzureOpenAI = $env:AZURE_OPENAI_ENDPOINT -and $env:AZURE_OPENAI_KEY

        if ($PSCmdlet.ParameterSetName -eq 'JumpId') {
            Write-Error "Azure Storage integration not yet implemented. Use -Jump parameter with a parsed jump object."
            return
        }

        if (-not $hasAzureOpenAI) {
            Write-Host "`n⚠ Azure OpenAI not configured - generating mock analysis" -ForegroundColor Yellow
            Write-Host "To enable AI analysis, set environment variables:" -ForegroundColor Gray
            Write-Host "  `$env:AZURE_OPENAI_ENDPOINT = 'your-endpoint'" -ForegroundColor Gray
            Write-Host "  `$env:AZURE_OPENAI_KEY = 'your-key'" -ForegroundColor Gray
            Write-Host ""

            # Generate mock analysis for demonstration
            $analysis = [PSCustomObject]@{
                JumpId = $Jump.JumpId
                FileName = $Jump.FileName
                OverallAssessment = "This jump demonstrates typical characteristics of a hop-n-pop skydive with a short freefall delay and extended canopy flight. The data quality is good with minor GPS acquisition noise at the beginning of the recording."
                SafetyFlags = @(
                    [PSCustomObject]@{
                        Category = "GPS Accuracy"
                        Description = "Initial GPS acquisition showed high horizontal accuracy errors (>100m). This is normal during satellite lock."
                        Severity = "Info"
                    }
                )
                Strengths = @(
                    "Stable aircraft climb phase with consistent ascent rate",
                    "Clean deployment signature with clear deceleration pattern",
                    "Extended canopy flight time for pattern work and approach",
                    "Smooth landing approach with gradual altitude loss"
                )
                ImprovementAreas = @(
                    "Consider waiting 1-2 minutes after GPS power-on before aircraft departure for better data quality",
                    "Extended freefall time would provide more performance data for analysis"
                )
                ProgressionRecommendation = "Based on this hop-n-pop profile, jumper appears comfortable with basic skills. Ready for progression to longer delays and more complex freefall work."
                SkillLevel = 6
                GeneratedBy = "Mock Analysis (AI not configured)"
                GeneratedAt = Get-Date
            }
        }
        else {
            Write-Host "`n🤖 Requesting AI analysis from Azure OpenAI..." -ForegroundColor Cyan
            Write-Error "Azure OpenAI integration not yet implemented in PowerShell module. This requires HTTP client implementation."
            return
        }

        # Display analysis
        Write-Host "`nAI Analysis for: $($Jump.FileName)" -ForegroundColor Cyan
        Write-Host ("=" * 60) -ForegroundColor Gray

        Write-Host "`nOverall Assessment:" -ForegroundColor Green
        Write-Host "  $($analysis.OverallAssessment)" -ForegroundColor White

        Write-Host "`nSkill Level: $($analysis.SkillLevel)/10" -ForegroundColor Green

        if ($analysis.SafetyFlags.Count -gt 0) {
            Write-Host "`nSafety Flags:" -ForegroundColor Yellow
            foreach ($flag in $analysis.SafetyFlags) {
                $color = switch ($flag.Severity) {
                    'Critical' { 'Red' }
                    'Warning' { 'Yellow' }
                    default { 'Gray' }
                }
                Write-Host "  [$($flag.Severity)] $($flag.Category)" -ForegroundColor $color
                Write-Host "    $($flag.Description)" -ForegroundColor White
            }
        }

        Write-Host "`nStrengths:" -ForegroundColor Green
        foreach ($strength in $analysis.Strengths) {
            Write-Host "  ✓ $strength" -ForegroundColor Green
        }

        if ($analysis.ImprovementAreas.Count -gt 0) {
            Write-Host "`nAreas for Improvement:" -ForegroundColor Yellow
            foreach ($area in $analysis.ImprovementAreas) {
                Write-Host "  → $area" -ForegroundColor Yellow
            }
        }

        Write-Host "`nProgression Recommendation:" -ForegroundColor Green
        Write-Host "  $($analysis.ProgressionRecommendation)" -ForegroundColor White

        Write-Host "`n" + ("=" * 60) -ForegroundColor Gray
        Write-Host "Analysis generated: $($analysis.GeneratedAt.ToString('yyyy-MM-dd HH:mm:ss'))" -ForegroundColor Gray
        Write-Host "Generated by: $($analysis.GeneratedBy)" -ForegroundColor Gray

        return $analysis
    }
    catch {
        Write-Error "Failed to generate jump analysis: $_"
        throw
    }
}
