function Export-JumpReport {
    <#
    .SYNOPSIS
        Generates a markdown report for a jump.
    .DESCRIPTION
        Creates a formatted markdown report containing metadata, metrics, AI analysis,
        and safety recommendations for a processed jump. The report can be used for
        logbook records, sharing with instructors, or personal tracking.
    .PARAMETER JumpId
        The unique identifier of the jump (for Azure-stored jumps).
    .PARAMETER Jump
        A jump object returned from Import-FlySightData.
    .PARAMETER Metrics
        Calculated metrics object from Get-JumpMetrics.
    .PARAMETER Analysis
        AI analysis object from Get-JumpAnalysis.
    .PARAMETER OutputPath
        Path where the markdown report will be saved.
    .PARAMETER Open
        Open the report in the default markdown viewer after creation.
    .OUTPUTS
        FileInfo object for the created report file.
    .EXAMPLE
        $jump = Import-FlySightData -Path .\samples\sample-jump.csv
        $metrics = Get-JumpMetrics -Jump $jump
        Export-JumpReport -Jump $jump -Metrics $metrics -OutputPath .\report.md
        
        Generate a report with metrics for a locally parsed jump.
    .EXAMPLE
        $jump = Import-FlySightData -Path .\samples\sample-jump.csv
        $metrics = Get-JumpMetrics -Jump $jump
        $analysis = Get-JumpAnalysis -Jump $jump -Metrics $metrics
        Export-JumpReport -Jump $jump -Metrics $metrics -Analysis $analysis -OutputPath .\report.md -Open
        
        Generate a complete report with AI analysis and open it.
    .EXAMPLE
        Export-JumpReport -JumpId 'a1b2c3d4-e5f6-7890-abcd-ef1234567890' -OutputPath .\report.md
        
        Generate a report for an Azure-stored jump.
    #>
    [CmdletBinding(DefaultParameterSetName = 'Jump')]
    param(
        [Parameter(Mandatory, ParameterSetName = 'JumpId')]
        [guid]$JumpId,

        [Parameter(Mandatory, ParameterSetName = 'Jump')]
        [PSCustomObject]$Jump,

        [Parameter(ParameterSetName = 'Jump')]
        [PSCustomObject]$Metrics,

        [Parameter(ParameterSetName = 'Jump')]
        [PSCustomObject]$Analysis,

        [Parameter(Mandatory)]
        [string]$OutputPath,

        [Parameter()]
        [switch]$Open
    )

    try {
        if ($PSCmdlet.ParameterSetName -eq 'JumpId') {
            Write-Error "Azure Storage integration not yet implemented. Use -Jump parameter with a parsed jump object."
            return
        }

        Write-Host "Generating jump report..." -ForegroundColor Cyan

        # Build markdown report
        $markdown = @"
# Jump Report

**Generated:** $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')  
**Jump ID:** $($Jump.JumpId)  
**File:** $($Jump.FileName)

---

## Flight Information

| Property | Value |
|---|---|
| Recording Start | $($Jump.Metadata.RecordingStart.ToString('yyyy-MM-dd HH:mm:ss')) UTC |
| Recording End | $($Jump.Metadata.RecordingEnd.ToString('HH:mm:ss')) UTC |
| Total Duration | $([Math]::Round(($Jump.Metadata.RecordingEnd - $Jump.Metadata.RecordingStart).TotalSeconds, 1))s |
| Data Points | $($Jump.Metadata.TotalDataPoints) |
| Recording Rate | 5 Hz |

## Altitude Profile

| Metric | Value |
|---|---|
| Maximum Altitude | $([Math]::Round($Jump.Metadata.MaxAltitude, 1))m MSL |
| Minimum Altitude (Ground) | $([Math]::Round($Jump.Metadata.MinAltitude, 1))m MSL |
| Total Altitude Range | $([Math]::Round($Jump.Metadata.MaxAltitude - $Jump.Metadata.MinAltitude, 1))m |

## Device Information

| Property | Value |
|---|---|
| FlySight Format | v$($Jump.Metadata.FormatVersion) |
| Firmware Version | $($Jump.Metadata.FirmwareVersion) |
| Device ID | $($Jump.Metadata.DeviceId) |
| Session ID | $($Jump.Metadata.SessionId) |

"@

        # Add metrics if provided
        if ($Metrics -and $Metrics.Segments) {
            $markdown += @"

---

## Performance Metrics

### Flight Segments

"@
            foreach ($segment in $Metrics.Segments) {
                $markdown += @"

#### $($segment.Phase)

| Metric | Value |
|---|---|
| Duration | $($segment.Duration)s |
| Start Altitude | $($segment.StartAltitude)m MSL |
| End Altitude | $($segment.EndAltitude)m MSL |
| Altitude Lost | $($segment.AltitudeLost)m |
| Start Time | $($segment.StartTime.ToString('HH:mm:ss')) |
| End Time | $($segment.EndTime.ToString('HH:mm:ss')) |

"@
                if ($segment.Metrics) {
                    $markdown += "**Performance:**`n`n"
                    foreach ($metric in $segment.Metrics.PSObject.Properties) {
                        $markdown += "- **$($metric.Name):** $($metric.Value)`n"
                    }
                    $markdown += "`n"
                }
            }
        }

        # Add AI analysis if provided
        if ($Analysis) {
            $markdown += @"

---

## AI Analysis

### Overall Assessment

$($Analysis.OverallAssessment)

### Skill Level

**Rating:** $($Analysis.SkillLevel)/10

"@

            if ($Analysis.SafetyFlags -and $Analysis.SafetyFlags.Count -gt 0) {
                $markdown += @"

### Safety Flags

"@
                foreach ($flag in $Analysis.SafetyFlags) {
                    $emoji = switch ($flag.Severity) {
                        'Critical' { '🔴' }
                        'Warning' { '⚠️' }
                        default { 'ℹ️' }
                    }
                    $markdown += @"

$emoji **[$($flag.Severity)] $($flag.Category)**  
$($flag.Description)

"@
                }
            }

            if ($Analysis.Strengths -and $Analysis.Strengths.Count -gt 0) {
                $markdown += @"

### Strengths

"@
                foreach ($strength in $Analysis.Strengths) {
                    $markdown += "- ✓ $strength`n"
                }
            }

            if ($Analysis.ImprovementAreas -and $Analysis.ImprovementAreas.Count -gt 0) {
                $markdown += @"

### Areas for Improvement

"@
                foreach ($area in $Analysis.ImprovementAreas) {
                    $markdown += "- → $area`n"
                }
            }

            $markdown += @"

### Progression Recommendation

$($Analysis.ProgressionRecommendation)

"@
        }

        # Add data quality section
        if ($Jump.ValidationResults) {
            $markdown += @"

---

## Data Quality

"@
            if ($Jump.ValidationResults.Errors.Count -gt 0) {
                $markdown += @"

### Errors

"@
                foreach ($error in $Jump.ValidationResults.Errors) {
                    $markdown += "- ❌ $error`n"
                }
            }

            if ($Jump.ValidationResults.Warnings.Count -gt 0) {
                $markdown += @"

### Warnings

"@
                foreach ($warning in $Jump.ValidationResults.Warnings) {
                    $markdown += "- ⚠️ $warning`n"
                }
            }

            if ($Jump.ValidationResults.IsValid -and $Jump.ValidationResults.Warnings.Count -eq 0) {
                $markdown += "✓ No data quality issues detected.`n"
            }
        }

        # Add footer
        $markdown += @"

---

*Report generated by JumpMetrics AI PowerShell Module*  
*https://github.com/ShaydeNofziger/jumpmetrics-ai*
"@

        # Write report to file
        $resolvedPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputPath)
        $markdown | Out-File -FilePath $resolvedPath -Encoding UTF8 -Force

        Write-Host "✓ Report saved to: $resolvedPath" -ForegroundColor Green

        # Open if requested
        if ($Open) {
            if ($IsWindows -or $PSVersionTable.PSVersion.Major -lt 6) {
                Start-Process $resolvedPath
            }
            elseif ($IsMacOS) {
                & open $resolvedPath
            }
            elseif ($IsLinux) {
                & xdg-open $resolvedPath 2>$null
                if ($LASTEXITCODE -ne 0) {
                    Write-Warning "Could not open file automatically. Please open manually: $resolvedPath"
                }
            }
        }

        return Get-Item -Path $resolvedPath
    }
    catch {
        Write-Error "Failed to generate jump report: $_"
        throw
    }
}
