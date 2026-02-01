function Get-JumpAnalysis {
    <#
    .SYNOPSIS
        Retrieves AI-powered analysis for a jump.
    .DESCRIPTION
        Calls the Azure OpenAI analysis agent to provide performance assessment,
        safety flags, and progression recommendations for a processed jump.
        
        If -JumpData is provided, displays analysis from a local jump object (if available).
        If -FunctionUrl and -JumpId are provided, retrieves analysis from Azure Storage.
    .PARAMETER JumpId
        The unique identifier of the jump to analyze (retrieved from Azure Storage).
    .PARAMETER FunctionUrl
        Base URL of the Azure Function API (e.g., "https://jumpmetrics.azurewebsites.net").
        The function will append "/api/jumps/{jumpId}/analysis" to this URL.
    .PARAMETER FunctionKey
        Function key for authentication (if required by the Azure Function).
    .PARAMETER JumpData
        A jump object returned from Import-FlySightData (for displaying local analysis if available).
    .OUTPUTS
        PSCustomObject with AI analysis including OverallAssessment, SafetyFlags, Strengths, ImprovementAreas, and ProgressionRecommendation.
    .EXAMPLE
        Get-JumpAnalysis -JumpData $jump
        
        Displays AI analysis from a jump object (if analysis was included in the response).
    .EXAMPLE
        Get-JumpAnalysis -JumpId 'a1b2c3d4-e5f6-7890-abcd-ef1234567890' -FunctionUrl "http://localhost:7071"
        
        Retrieves AI analysis from Azure Storage via the local Function API.
    .EXAMPLE
        Import-FlySightData -Path .\jump.csv -FunctionUrl "http://localhost:7071/api/jumps/analyze" | Get-JumpAnalysis
        
        Pipeline example: imports a jump and displays its AI analysis.
    #>
    [CmdletBinding(DefaultParameterSetName = 'FromJumpData')]
    param(
        [Parameter(Mandatory, ParameterSetName = 'FromStorage')]
        [guid]$JumpId,

        [Parameter(Mandatory, ParameterSetName = 'FromStorage')]
        [string]$FunctionUrl,

        [Parameter(ParameterSetName = 'FromStorage')]
        [string]$FunctionKey,

        [Parameter(Mandatory, ParameterSetName = 'FromJumpData', ValueFromPipeline)]
        [PSCustomObject]$JumpData
    )

    process {
        try {
            $analysis = $null

            if ($PSCmdlet.ParameterSetName -eq 'FromStorage') {
                Write-Verbose "Retrieving AI analysis for Jump ID: $JumpId"
                
                # Construct API URL
                $apiUrl = "$($FunctionUrl.TrimEnd('/'))/api/jumps/$JumpId/analysis"
                
                # Prepare headers
                $headers = @{}
                if (-not [string]::IsNullOrEmpty($FunctionKey)) {
                    $headers['x-functions-key'] = $FunctionKey
                }
                
                try {
                    $response = Invoke-RestMethod -Uri $apiUrl -Method Get -Headers $headers -ErrorAction Stop
                    $analysis = $response.analysis
                }
                catch {
                    Write-Error "Failed to retrieve AI analysis from Azure: $_"
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
                else {
                    Write-Warning "No AI analysis found in jump data."
                    Write-Host "  → AI analysis requires Azure OpenAI integration" -ForegroundColor Yellow
                    Write-Host "     The jump was processed but AI analysis was not performed" -ForegroundColor Gray
                    return
                }
            }

            if ($null -eq $analysis) {
                Write-Warning "No AI analysis available for this jump."
                Write-Host "  → AI analysis may not have been generated yet or requires Azure OpenAI configuration" -ForegroundColor Yellow
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
