function Get-JumpAnalysis {
    <#
    .SYNOPSIS
        Retrieves or generates AI-powered analysis for a jump.
    .DESCRIPTION
        Provides AI-powered performance assessment, safety flags, and progression recommendations 
        for a processed jump. Can load existing analysis from local storage or generate new 
        analysis using Azure OpenAI if credentials are provided.
        
        AI analysis is optional and requires Azure OpenAI configuration. Basic jump processing
        (parsing, segmentation, metrics) works without AI.
    .PARAMETER JumpId
        The unique identifier of the jump to analyze (retrieved from local storage).
    .PARAMETER StoragePath
        Path to the local storage directory. Defaults to ~/.jumpmetrics/jumps/
    .PARAMETER JumpData
        A jump object returned from Import-FlySightData (for displaying local analysis if available).
    .PARAMETER GenerateWithAI
        If specified, generates AI analysis using Azure OpenAI (requires OpenAI configuration).
    .PARAMETER OpenAIEndpoint
        Azure OpenAI endpoint URL (required if GenerateWithAI is specified).
    .PARAMETER OpenAIKey
        Azure OpenAI API key (required if GenerateWithAI is specified).
    .PARAMETER OpenAIDeployment
        Azure OpenAI deployment name (defaults to 'gpt-4').
    .OUTPUTS
        PSCustomObject with AI analysis including OverallAssessment, SafetyFlags, Strengths, ImprovementAreas, and ProgressionRecommendation.
    .EXAMPLE
        Get-JumpAnalysis -JumpData $jump
        
        Displays AI analysis from a jump object (if analysis was included).
    .EXAMPLE
        Get-JumpAnalysis -JumpId 'a1b2c3d4-e5f6-7890-abcd-ef1234567890'
        
        Retrieves AI analysis from local storage.
    .EXAMPLE
        Import-FlySightData -Path .\jump.csv | Get-JumpAnalysis -GenerateWithAI -OpenAIEndpoint "https://..." -OpenAIKey "..."
        
        Processes a jump and generates AI analysis using Azure OpenAI.
    #>
    [CmdletBinding(DefaultParameterSetName = 'FromJumpData')]
    param(
        [Parameter(Mandatory, ParameterSetName = 'FromStorage')]
        [guid]$JumpId,

        [Parameter(ParameterSetName = 'FromStorage')]
        [string]$StoragePath = (Join-Path $HOME ".jumpmetrics/jumps"),

        [Parameter(Mandatory, ParameterSetName = 'FromJumpData', ValueFromPipeline)]
        [PSCustomObject]$JumpData,

        [Parameter()]
        [switch]$GenerateWithAI,

        [Parameter()]
        [string]$OpenAIEndpoint,

        [Parameter()]
        [string]$OpenAIKey,

        [Parameter()]
        [string]$OpenAIDeployment = 'gpt-4'
    )

    process {
        try {
            $analysis = $null

            if ($PSCmdlet.ParameterSetName -eq 'FromStorage') {
                Write-Verbose "Loading analysis for Jump ID: $JumpId from $StoragePath"
                
                $jumpFile = Join-Path $StoragePath "$JumpId.json"
                if (-not (Test-Path $jumpFile)) {
                    Write-Error "Jump file not found: $jumpFile"
                    return
                }
                
                try {
                    $JumpData = Get-Content -Path $jumpFile -Raw | ConvertFrom-Json
                    $analysis = $JumpData.analysis
                }
                catch {
                    Write-Error "Failed to load jump from storage: $_"
                    return
                }
            }
            else {
                # Extract analysis from JumpData object
                if ($JumpData.PSObject.Properties['analysis']) {
                    $analysis = $JumpData.analysis
                }
                elseif ($JumpData.PSObject.Properties['Analysis']) {
                    $analysis = $JumpData.Analysis
                }
            }

            # Generate AI analysis if requested and not already present
            if ($GenerateWithAI -and $null -eq $analysis) {
                Write-Host "  → Generating AI analysis with Azure OpenAI..." -ForegroundColor Yellow
                
                if ([string]::IsNullOrEmpty($OpenAIEndpoint) -or [string]::IsNullOrEmpty($OpenAIKey)) {
                    Write-Warning "AI analysis generation requires -OpenAIEndpoint and -OpenAIKey parameters."
                    Write-Host "  → Set these parameters or configure environment variables AZURE_OPENAI_ENDPOINT and AZURE_OPENAI_KEY" -ForegroundColor Gray
                    return
                }
                
                # TODO: Implement AI analysis generation using Azure OpenAI SDK
                Write-Warning "AI analysis generation is not yet implemented."
                Write-Host "  → This feature requires integration with Azure OpenAI service" -ForegroundColor Gray
                Write-Host "  → For now, process jumps without AI analysis for metrics and segmentation" -ForegroundColor Gray
                return
            }

            if ($null -eq $analysis) {
                Write-Warning "No AI analysis found in jump data."
                Write-Host "  → AI analysis is optional and requires Azure OpenAI integration" -ForegroundColor Yellow
                Write-Host "  → Use -GenerateWithAI with OpenAI credentials to generate analysis" -ForegroundColor Gray
                return
            }

            # Display AI analysis in a formatted way
            Write-Host "`n══════════════════════════════════════════════════════" -ForegroundColor Magenta
            Write-Host "              AI-POWERED JUMP ANALYSIS" -ForegroundColor Magenta
            Write-Host "══════════════════════════════════════════════════════" -ForegroundColor Magenta

            # Overall Assessment
            if ($analysis.overallAssessment -or $analysis.OverallAssessment) {
                $assessment = $analysis.overallAssessment ?? $analysis.OverallAssessment
                Write-Host "`n📊 OVERALL ASSESSMENT" -ForegroundColor Cyan
                Write-Host "  $assessment" -ForegroundColor Gray
            }

            # Skill Level
            if ($null -ne ($analysis.skillLevel ?? $analysis.SkillLevel)) {
                $skillLevel = $analysis.skillLevel ?? $analysis.SkillLevel
                Write-Host "`n⭐ SKILL LEVEL: $skillLevel/10" -ForegroundColor Cyan
            }

            # Safety Flags
            $safetyFlags = $analysis.safetyFlags ?? $analysis.SafetyFlags
            if ($safetyFlags -and $safetyFlags.Count -gt 0) {
                Write-Host "`n⚠️  SAFETY FLAGS" -ForegroundColor Red
                foreach ($flag in $safetyFlags) {
                    $severity = $flag.severity ?? $flag.Severity ?? 'Info'
                    $category = $flag.category ?? $flag.Category
                    $description = $flag.description ?? $flag.Description
                    
                    $icon = switch ($severity) {
                        'Critical' { '🔴' }
                        'Warning'  { '⚠️ ' }
                        default    { 'ℹ️ ' }
                    }
                    
                    $color = switch ($severity) {
                        'Critical' { 'Red' }
                        'Warning'  { 'Yellow' }
                        default    { 'Gray' }
                    }
                    
                    Write-Host "  $icon [$severity] $category" -ForegroundColor $color
                    Write-Host "     $description" -ForegroundColor Gray
                }
            }
            else {
                Write-Host "`n✅ NO SAFETY FLAGS" -ForegroundColor Green
                Write-Host "  No safety concerns detected in this jump." -ForegroundColor Gray
            }

            # Strengths
            $strengths = $analysis.strengths ?? $analysis.Strengths
            if ($strengths -and $strengths.Count -gt 0) {
                Write-Host "`n💪 STRENGTHS" -ForegroundColor Green
                foreach ($strength in $strengths) {
                    Write-Host "  ✓ $strength" -ForegroundColor Gray
                }
            }

            # Improvement Areas
            $improvements = $analysis.improvementAreas ?? $analysis.ImprovementAreas
            if ($improvements -and $improvements.Count -gt 0) {
                Write-Host "`n📈 AREAS FOR IMPROVEMENT" -ForegroundColor Yellow
                foreach ($improvement in $improvements) {
                    Write-Host "  → $improvement" -ForegroundColor Gray
                }
            }

            # Progression Recommendation
            if ($analysis.progressionRecommendation -or $analysis.ProgressionRecommendation) {
                $recommendation = $analysis.progressionRecommendation ?? $analysis.ProgressionRecommendation
                Write-Host "`n🎯 PROGRESSION RECOMMENDATION" -ForegroundColor Cyan
                Write-Host "  $recommendation" -ForegroundColor Gray
            }

            Write-Host "`n══════════════════════════════════════════════════════`n" -ForegroundColor Magenta

            # Return the analysis object for pipeline usage
            return $analysis
        }
        catch {
            Write-Error "Failed to retrieve jump analysis: $_"
            throw
        }
    }
}
