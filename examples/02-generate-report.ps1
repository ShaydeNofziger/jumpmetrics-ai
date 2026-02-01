#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generate a complete markdown report from a FlySight file.

.DESCRIPTION
    This example shows how to process a FlySight file and generate a comprehensive
    markdown report with metrics, AI analysis, and safety recommendations.

.PARAMETER InputPath
    Path to the FlySight CSV file to process.

.PARAMETER OutputPath
    Path where the markdown report will be saved.

.PARAMETER Open
    Open the report in the default markdown viewer after generation.

.EXAMPLE
    ./examples/02-generate-report.ps1 -InputPath ./samples/sample-jump.csv -OutputPath ./report.md

.EXAMPLE
    ./examples/02-generate-report.ps1 -InputPath ./samples/sample-jump.csv -OutputPath ./report.md -Open

.NOTES
    Requires PowerShell 7.5+ and the JumpMetrics module.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$InputPath,

    [Parameter(Mandatory)]
    [string]$OutputPath,

    [switch]$Open
)

# Import the JumpMetrics module
Import-Module ./src/JumpMetrics.PowerShell/JumpMetrics.psm1 -Force

Write-Host "`n=== JumpMetrics AI - Report Generation Example ===" -ForegroundColor Cyan
Write-Host "Processing: $InputPath" -ForegroundColor Gray
Write-Host "Output: $OutputPath`n" -ForegroundColor Gray

try {
    # Process the jump file
    Write-Host "Importing FlySight data..." -ForegroundColor Yellow
    $jump = Import-FlySightData -Path $InputPath

    Write-Host "`nCalculating metrics..." -ForegroundColor Yellow
    $metrics = Get-JumpMetrics -Jump $jump

    Write-Host "`nGenerating AI analysis..." -ForegroundColor Yellow
    $analysis = Get-JumpAnalysis -Jump $jump -Metrics $metrics

    Write-Host "`nExporting report..." -ForegroundColor Yellow
    $reportFile = Export-JumpReport -Jump $jump -Metrics $metrics -Analysis $analysis -OutputPath $OutputPath -Open:$Open

    Write-Host "`n=== Report Generated Successfully ===" -ForegroundColor Green
    Write-Host "Report saved to: $($reportFile.FullName)" -ForegroundColor White
}
catch {
    Write-Error "Failed to generate report: $_"
    exit 1
}
