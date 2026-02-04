<#
.SYNOPSIS
    Example 1: Local FlySight CSV Analysis

.DESCRIPTION
    This example demonstrates how to parse and analyze a FlySight 2 CSV file
    locally using the complete processing pipeline: parsing, validation, 
    segmentation, and metrics calculation. All processing is done locally
    without requiring Azure services.

.NOTES
    This is a fully local operation that does not require any cloud credentials.
    AI analysis is optional and requires Azure OpenAI configuration.
#>

# Import the JumpMetrics module
Import-Module "$PSScriptRoot/../src/JumpMetrics.PowerShell/JumpMetrics.psm1" -Force

# Process a FlySight CSV file locally
Write-Host "`n=== LOCAL FLYSIGHT ANALYSIS ===" -ForegroundColor Cyan
Write-Host "Processing FlySight data with complete local pipeline`n" -ForegroundColor Gray

$jumpData = Import-FlySightData -Path "$PSScriptRoot/../samples/sample-jump.csv" -Verbose

# Display detailed metrics
Write-Host "`n=== DETAILED METRICS ===" -ForegroundColor Cyan
Get-JumpMetrics -JumpData $jumpData

# Generate a markdown report
Write-Host "`n=== GENERATING REPORT ===" -ForegroundColor Cyan
$reportPath = "$PSScriptRoot/../reports/local-analysis-report.md"
Export-JumpReport -JumpData $jumpData -OutputPath $reportPath

Write-Host "`n✓ Analysis complete!" -ForegroundColor Green
Write-Host "  View the report at: $reportPath`n" -ForegroundColor Gray

