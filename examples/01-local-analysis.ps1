<#
.SYNOPSIS
    Example 1: Local FlySight CSV Analysis (Offline Mode)

.DESCRIPTION
    This example demonstrates how to parse and analyze a FlySight 2 CSV file
    locally without requiring Azure Function connectivity. Perfect for offline
    analysis or initial data exploration.

.NOTES
    This is a read-only operation that does not require Azure credentials.
#>

# Import the JumpMetrics module
Import-Module "$PSScriptRoot/../src/JumpMetrics.PowerShell/JumpMetrics.psm1" -Force

# Parse a FlySight CSV file locally
Write-Host "`n=== LOCAL FLYSIGHT ANALYSIS ===" -ForegroundColor Cyan
Write-Host "This example parses FlySight data without uploading to Azure`n" -ForegroundColor Gray

$jumpData = Import-FlySightData -Path "$PSScriptRoot/../samples/sample-jump.csv" -LocalOnly -Verbose

# Display summary
Write-Host "`n=== JUMP SUMMARY ===" -ForegroundColor Cyan
Write-Host "Device ID:        $($jumpData.Metadata.DeviceId)" -ForegroundColor Gray
Write-Host "Firmware:         $($jumpData.Metadata.FirmwareVersion)" -ForegroundColor Gray
Write-Host "Data Points:      $($jumpData.Metadata.TotalDataPoints)" -ForegroundColor Gray
Write-Host "Recording Start:  $($jumpData.Metadata.RecordingStart)" -ForegroundColor Gray
Write-Host "Recording End:    $($jumpData.Metadata.RecordingEnd)" -ForegroundColor Gray
Write-Host "Max Altitude:     $([Math]::Round($jumpData.Metadata.MaxAltitude, 1))m MSL" -ForegroundColor Gray
Write-Host "Min Altitude:     $([Math]::Round($jumpData.Metadata.MinAltitude, 1))m MSL" -ForegroundColor Gray

# Generate a markdown report
Write-Host "`n=== GENERATING REPORT ===" -ForegroundColor Cyan
$reportPath = "$PSScriptRoot/../reports/local-analysis-report.md"
Export-JumpReport -JumpData $jumpData -OutputPath $reportPath

Write-Host "`n✓ Analysis complete!" -ForegroundColor Green
Write-Host "  View the report at: $reportPath`n" -ForegroundColor Gray
