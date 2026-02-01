function Export-JumpReport {
    <#
    .SYNOPSIS
        Generates a markdown report for a jump.
    .DESCRIPTION
        Creates a formatted markdown report containing metadata, segments, metrics, 
        and AI analysis (if available) for a processed jump.
        
        The report can be generated from a local jump object (returned by Import-FlySightData)
        or retrieved from Azure Storage using JumpId and FunctionUrl.
    .PARAMETER JumpId
        The unique identifier of the jump (for retrieving from Azure Storage).
    .PARAMETER FunctionUrl
        Base URL of the Azure Function API (e.g., "https://jumpmetrics.azurewebsites.net").
    .PARAMETER FunctionKey
        Function key for authentication (if required by the Azure Function).
    .PARAMETER JumpData
        A jump object returned from Import-FlySightData (for generating report from local data).
    .PARAMETER OutputPath
        Path where the markdown report will be saved.
    .OUTPUTS
        String path to the generated report file.
    .EXAMPLE
        Export-JumpReport -JumpData $jump -OutputPath .\reports\jump-report.md
        
        Generates a report from a local jump object.
    .EXAMPLE
        Export-JumpReport -JumpId 'a1b2c3d4-e5f6-7890-abcd-ef1234567890' -FunctionUrl "http://localhost:7071" -OutputPath .\report.md
        
        Retrieves jump data from Azure and generates a report.
    .EXAMPLE
        Import-FlySightData -Path .\jump.csv -FunctionUrl "http://localhost:7071/api/jumps/analyze" | Export-JumpReport -OutputPath .\report.md
        
        Pipeline example: imports a jump and generates a report.
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
        $JumpData,

        [Parameter(Mandatory, ParameterSetName = 'FromStorage')]
        [Parameter(Mandatory, ParameterSetName = 'FromJumpData')]
        [string]$OutputPath
    )

    process {
        try {
            if ($PSCmdlet.ParameterSetName -eq 'FromStorage') {
                Write-Verbose "Retrieving jump data for Jump ID: $JumpId"
                
                # Construct API URL
                $apiUrl = "$($FunctionUrl.TrimEnd('/'))/api/jumps/$JumpId"
                
                # Prepare headers
                $headers = @{}
                if (-not [string]::IsNullOrEmpty($FunctionKey)) {
                    $headers['x-functions-key'] = $FunctionKey
                }
                
                try {
                    $JumpData = Invoke-RestMethod -Uri $apiUrl -Method Get -Headers $headers -ErrorAction Stop
                }
                catch {
                    Write-Error "Failed to retrieve jump data from Azure: $_"
                    return
                }
            }
            else {
                Write-Verbose "Received JumpData parameter. Type: $($JumpData.GetType().Name)"
                Write-Verbose "JumpData has properties: $($JumpData.PSObject.Properties.Name -join ', ')"
            }

            Write-Verbose "After parameter handling, JumpData is null: $($null -eq $JumpData)"

            if ($null -eq $JumpData) {
                Write-Error "No jump data available to generate report."
                return
            }

            # Check if this is a full jump object or just local parse results
            $isLocalParseOnly = ($null -ne $JumpData.DataPoints) -and 
                                ($null -eq $JumpData.segments) -and 
                                ($null -eq $JumpData.Segments)

            if ($isLocalParseOnly) {
                Write-Warning "Generating report from local parse data only (no segments or metrics)."
                Write-Host "  → For full analysis, use Import-FlySightData with -FunctionUrl" -ForegroundColor Yellow
            }

            Write-Verbose "Generating markdown report..."
            Write-Host "Generating jump report..." -ForegroundColor Cyan

            # Build markdown report
            $report = [System.Text.StringBuilder]::new()
            
            # Header
            [void]$report.AppendLine("# Jump Analysis Report")
            [void]$report.AppendLine()
            
            # Metadata Section
            [void]$report.AppendLine("## Jump Metadata")
            [void]$report.AppendLine()
            
            if ($JumpData.jumpId -or $JumpData.JumpId) {
                [void]$report.AppendLine("**Jump ID:** ``$($JumpData.jumpId ?? $JumpData.JumpId)``")
            }
            if ($JumpData.jumpDate -or $JumpData.JumpDate) {
                $date = $JumpData.jumpDate ?? $JumpData.JumpDate
                [void]$report.AppendLine("**Jump Date:** $date")
            }
            if ($JumpData.fileName -or $JumpData.flySightFileName -or $JumpData.FlySightFileName) {
                $fileName = $JumpData.fileName ?? $JumpData.flySightFileName ?? $JumpData.FlySightFileName
                [void]$report.AppendLine("**File Name:** ``$fileName``")
            }
            [void]$report.AppendLine()
            
            # Recording Details
            $metadata = $JumpData.metadata ?? $JumpData.Metadata
            if ($metadata) {
                [void]$report.AppendLine("### Recording Details")
                [void]$report.AppendLine()
                [void]$report.AppendLine("| Property | Value |")
                [void]$report.AppendLine("|----------|-------|")
                
                if ($metadata.totalDataPoints -or $metadata.TotalDataPoints) {
                    [void]$report.AppendLine("| Data Points | $($metadata.totalDataPoints ?? $metadata.TotalDataPoints) |")
                }
                if ($metadata.recordingStart -or $metadata.RecordingStart) {
                    [void]$report.AppendLine("| Recording Start | $($metadata.recordingStart ?? $metadata.RecordingStart) |")
                }
                if ($metadata.recordingEnd -or $metadata.RecordingEnd) {
                    [void]$report.AppendLine("| Recording End | $($metadata.recordingEnd ?? $metadata.RecordingEnd) |")
                }
                if ($null -ne ($metadata.maxAltitude ?? $metadata.MaxAltitude)) {
                    [void]$report.AppendLine("| Max Altitude | $([Math]::Round($metadata.maxAltitude ?? $metadata.MaxAltitude, 1)) m MSL |")
                }
                if ($null -ne ($metadata.minAltitude ?? $metadata.MinAltitude)) {
                    [void]$report.AppendLine("| Min Altitude | $([Math]::Round($metadata.minAltitude ?? $metadata.MinAltitude, 1)) m MSL |")
                }
                if ($metadata.firmwareVersion -or $metadata.FirmwareVersion) {
                    [void]$report.AppendLine("| FlySight Firmware | $($metadata.firmwareVersion ?? $metadata.FirmwareVersion) |")
                }
                if ($metadata.deviceId -or $metadata.DeviceId) {
                    [void]$report.AppendLine("| Device ID | ``$($metadata.deviceId ?? $metadata.DeviceId)`` |")
                }
                [void]$report.AppendLine()
            }
            
            # Segments Section
            $segments = $JumpData.segments ?? $JumpData.Segments
            if ($segments -and $segments.Count -gt 0) {
                [void]$report.AppendLine("## Jump Segments")
                [void]$report.AppendLine()
                [void]$report.AppendLine("| Phase | Duration | Start Altitude | End Altitude | Altitude Loss |")
                [void]$report.AppendLine("|-------|----------|----------------|--------------|---------------|")
                
                foreach ($segment in $segments) {
                    $type = $segment.type ?? $segment.Type
                    $duration = [Math]::Round(($segment.duration ?? $segment.Duration), 1)
                    $startAlt = [Math]::Round(($segment.startAltitude ?? $segment.StartAltitude), 0)
                    $endAlt = [Math]::Round(($segment.endAltitude ?? $segment.EndAltitude), 0)
                    $altLoss = [Math]::Round($startAlt - $endAlt, 0)
                    
                    [void]$report.AppendLine("| $type | ${duration}s | ${startAlt}m | ${endAlt}m | ${altLoss}m |")
                }
                [void]$report.AppendLine()
            }
            
            # Performance Metrics Section
            $metrics = $JumpData.metrics ?? $JumpData.Metrics
            if ($metrics) {
                [void]$report.AppendLine("## Performance Metrics")
                [void]$report.AppendLine()
                
                # Freefall
                $freefall = $metrics.freefall ?? $metrics.Freefall
                if ($freefall) {
                    [void]$report.AppendLine("### Freefall")
                    [void]$report.AppendLine()
                    [void]$report.AppendLine("| Metric | Value |")
                    [void]$report.AppendLine("|--------|-------|")
                    [void]$report.AppendLine("| Time in Freefall | $([Math]::Round($freefall.timeInFreefall ?? $freefall.TimeInFreefall, 1)) seconds |")
                    [void]$report.AppendLine("| Average Vertical Speed | $([Math]::Round($freefall.averageVerticalSpeed ?? $freefall.AverageVerticalSpeed, 1)) m/s |")
                    [void]$report.AppendLine("| Maximum Vertical Speed | $([Math]::Round($freefall.maxVerticalSpeed ?? $freefall.MaxVerticalSpeed, 1)) m/s |")
                    [void]$report.AppendLine("| Average Horizontal Speed | $([Math]::Round($freefall.averageHorizontalSpeed ?? $freefall.AverageHorizontalSpeed, 1)) m/s |")
                    if ($null -ne ($freefall.trackAngle ?? $freefall.TrackAngle)) {
                        [void]$report.AppendLine("| Track Angle | $([Math]::Round($freefall.trackAngle ?? $freefall.TrackAngle, 1))° |")
                    }
                    [void]$report.AppendLine()
                }
                
                # Canopy
                $canopy = $metrics.canopy ?? $metrics.Canopy
                if ($canopy) {
                    [void]$report.AppendLine("### Canopy Flight")
                    [void]$report.AppendLine()
                    [void]$report.AppendLine("| Metric | Value |")
                    [void]$report.AppendLine("|--------|-------|")
                    [void]$report.AppendLine("| Deployment Altitude | $([Math]::Round($canopy.deploymentAltitude ?? $canopy.DeploymentAltitude, 0)) m MSL |")
                    [void]$report.AppendLine("| Average Descent Rate | $([Math]::Round($canopy.averageDescentRate ?? $canopy.AverageDescentRate, 1)) m/s |")
                    [void]$report.AppendLine("| Glide Ratio | $([Math]::Round($canopy.glideRatio ?? $canopy.GlideRatio, 2)):1 |")
                    [void]$report.AppendLine("| Maximum Horizontal Speed | $([Math]::Round($canopy.maxHorizontalSpeed ?? $canopy.MaxHorizontalSpeed, 1)) m/s |")
                    [void]$report.AppendLine("| Total Canopy Time | $([Math]::Round($canopy.totalCanopyTime ?? $canopy.TotalCanopyTime, 1)) seconds |")
                    if ($null -ne ($canopy.patternAltitude ?? $canopy.PatternAltitude)) {
                        [void]$report.AppendLine("| Pattern Entry Altitude | $([Math]::Round($canopy.patternAltitude ?? $canopy.PatternAltitude, 0)) m MSL |")
                    }
                    [void]$report.AppendLine()
                }
                
                # Landing
                $landing = $metrics.landing ?? $metrics.Landing
                if ($landing) {
                    [void]$report.AppendLine("### Landing")
                    [void]$report.AppendLine()
                    [void]$report.AppendLine("| Metric | Value |")
                    [void]$report.AppendLine("|--------|-------|")
                    [void]$report.AppendLine("| Final Approach Speed | $([Math]::Round($landing.finalApproachSpeed ?? $landing.FinalApproachSpeed, 1)) m/s |")
                    [void]$report.AppendLine("| Touchdown Vertical Speed | $([Math]::Round($landing.touchdownVerticalSpeed ?? $landing.TouchdownVerticalSpeed, 1)) m/s |")
                    if ($null -ne ($landing.landingAccuracy ?? $landing.LandingAccuracy)) {
                        [void]$report.AppendLine("| Landing Accuracy | $([Math]::Round($landing.landingAccuracy ?? $landing.LandingAccuracy, 1)) m |")
                    }
                    [void]$report.AppendLine()
                }
            }
            
            # AI Analysis Section
            $analysis = $JumpData.analysis ?? $JumpData.Analysis
            if ($analysis) {
                [void]$report.AppendLine("## AI Analysis")
                [void]$report.AppendLine()
                
                # Overall Assessment
                if ($analysis.overallAssessment -or $analysis.OverallAssessment) {
                    [void]$report.AppendLine("### Overall Assessment")
                    [void]$report.AppendLine()
                    [void]$report.AppendLine($analysis.overallAssessment ?? $analysis.OverallAssessment)
                    [void]$report.AppendLine()
                }
                
                # Skill Level
                if ($null -ne ($analysis.skillLevel ?? $analysis.SkillLevel)) {
                    [void]$report.AppendLine("**Skill Level:** $($analysis.skillLevel ?? $analysis.SkillLevel)/10")
                    [void]$report.AppendLine()
                }
                
                # Safety Flags
                $safetyFlags = $analysis.safetyFlags ?? $analysis.SafetyFlags
                if ($safetyFlags -and $safetyFlags.Count -gt 0) {
                    [void]$report.AppendLine("### Safety Flags")
                    [void]$report.AppendLine()
                    foreach ($flag in $safetyFlags) {
                        $severity = $flag.severity ?? $flag.Severity ?? 'Info'
                        $category = $flag.category ?? $flag.Category
                        $description = $flag.description ?? $flag.Description
                        
                        $icon = switch ($severity) {
                            'Critical' { '🔴' }
                            'Warning'  { '⚠️' }
                            default    { 'ℹ️' }
                        }
                        
                        $line = "- $icon **``[$severity``] ${category}:** $description"
                        [void]$report.AppendLine($line)
                    }
                    [void]$report.AppendLine()
                }
                
                # Strengths
                $strengths = $analysis.strengths ?? $analysis.Strengths
                if ($strengths -and $strengths.Count -gt 0) {
                    [void]$report.AppendLine("### Strengths")
                    [void]$report.AppendLine()
                    foreach ($strength in $strengths) {
                        [void]$report.AppendLine("- ✓ $strength")
                    }
                    [void]$report.AppendLine()
                }
                
                # Improvement Areas
                $improvements = $analysis.improvementAreas ?? $analysis.ImprovementAreas
                if ($improvements -and $improvements.Count -gt 0) {
                    [void]$report.AppendLine("### Areas for Improvement")
                    [void]$report.AppendLine()
                    foreach ($improvement in $improvements) {
                        [void]$report.AppendLine("- → $improvement")
                    }
                    [void]$report.AppendLine()
                }
                
                # Progression Recommendation
                if ($analysis.progressionRecommendation -or $analysis.ProgressionRecommendation) {
                    [void]$report.AppendLine("### Progression Recommendation")
                    [void]$report.AppendLine()
                    [void]$report.AppendLine($analysis.progressionRecommendation ?? $analysis.ProgressionRecommendation)
                    [void]$report.AppendLine()
                }
            }
            
            # Footer
            [void]$report.AppendLine("---")
            [void]$report.AppendLine()
            [void]$report.AppendLine("*Report generated by JumpMetrics AI on $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss UTC')*")
            
            # Write report to file
            $outputDir = Split-Path -Path $OutputPath -Parent
            if ($outputDir -and -not (Test-Path $outputDir)) {
                New-Item -Path $outputDir -ItemType Directory -Force | Out-Null
            }
            
            $report.ToString() | Out-File -FilePath $OutputPath -Encoding UTF8 -Force
            
            Write-Host "  ✓ Report generated successfully!" -ForegroundColor Green
            Write-Host "  → Output: $OutputPath" -ForegroundColor Cyan
            
            return (Resolve-Path $OutputPath).Path
        }
        catch {
            Write-Error "Failed to generate jump report: $_"
            throw
        }
    }
}
