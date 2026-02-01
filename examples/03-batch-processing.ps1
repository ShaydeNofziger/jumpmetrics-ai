<#
.SYNOPSIS
    Example 3: Batch Processing Multiple Jump Files

.DESCRIPTION
    This example demonstrates how to process multiple FlySight CSV files
    in batch, generating individual reports for each jump.

.PARAMETER SourceDirectory
    Directory containing FlySight CSV files to process.

.PARAMETER OutputDirectory
    Directory where markdown reports will be saved.

.PARAMETER LocalOnly
    If specified, processes files locally without uploading to Azure.
#>

param(
    [Parameter()]
    [string]$SourceDirectory = "$PSScriptRoot/../samples",
    
    [Parameter()]
    [string]$OutputDirectory = "$PSScriptRoot/../reports/batch",
    
    [Parameter()]
    [switch]$LocalOnly,
    
    [Parameter()]
    [string]$FunctionUrl,
    
    [Parameter()]
    [string]$FunctionKey
)

# Import the JumpMetrics module
Import-Module "$PSScriptRoot/../src/JumpMetrics.PowerShell/JumpMetrics.psm1" -Force

Write-Host "`n=== BATCH PROCESSING FLYSIGHT FILES ===" -ForegroundColor Cyan
Write-Host "Source:    $SourceDirectory" -ForegroundColor Gray
Write-Host "Output:    $OutputDirectory" -ForegroundColor Gray
Write-Host "Mode:      $(if ($LocalOnly) { 'Local Only' } else { 'Azure Processing' })`n" -ForegroundColor Gray

# Create output directory if it doesn't exist
if (-not (Test-Path $OutputDirectory)) {
    New-Item -Path $OutputDirectory -ItemType Directory -Force | Out-Null
}

# Find all CSV files
$csvFiles = Get-ChildItem -Path $SourceDirectory -Filter "*.csv" -File

if ($csvFiles.Count -eq 0) {
    Write-Warning "No CSV files found in $SourceDirectory"
    return
}

Write-Host "Found $($csvFiles.Count) CSV file(s) to process`n" -ForegroundColor Cyan

# Process each file
$results = @()
$successCount = 0
$failCount = 0

foreach ($file in $csvFiles) {
    Write-Host "Processing: $($file.Name)..." -ForegroundColor Yellow
    
    try {
        # Import the jump data
        $importParams = @{
            Path = $file.FullName
        }
        
        if ($LocalOnly) {
            $importParams['LocalOnly'] = $true
        }
        elseif ($FunctionUrl) {
            $importParams['FunctionUrl'] = $FunctionUrl
            if ($FunctionKey) {
                $importParams['FunctionKey'] = $FunctionKey
            }
        }
        else {
            Write-Warning "  Skipping - no FunctionUrl provided and LocalOnly not specified"
            continue
        }
        
        $jumpData = Import-FlySightData @importParams
        
        if ($jumpData) {
            # Generate report
            $baseName = [System.IO.Path]::GetFileNameWithoutExtension($file.Name)
            $reportPath = Join-Path $OutputDirectory "$baseName-report.md"
            
            Export-JumpReport -JumpData $jumpData -OutputPath $reportPath | Out-Null
            
            $results += [PSCustomObject]@{
                FileName = $file.Name
                Status = "Success"
                DataPoints = $jumpData.Metadata.TotalDataPoints ?? $jumpData.DataPoints.Count
                ReportPath = $reportPath
            }
            
            $successCount++
            Write-Host "  ✓ Success - Report: $reportPath" -ForegroundColor Green
        }
        else {
            throw "Import returned null"
        }
    }
    catch {
        $results += [PSCustomObject]@{
            FileName = $file.Name
            Status = "Failed"
            Error = $_.Exception.Message
            ReportPath = ""
        }
        
        $failCount++
        Write-Host "  ✗ Failed: $_" -ForegroundColor Red
    }
    
    Write-Host ""
}

# Display summary
Write-Host ("=" * 60) -ForegroundColor Cyan
Write-Host "BATCH PROCESSING SUMMARY" -ForegroundColor Cyan
Write-Host ("=" * 60) -ForegroundColor Cyan
Write-Host "Total Files:     $($csvFiles.Count)" -ForegroundColor Gray
Write-Host "Successful:      $successCount" -ForegroundColor Green
Write-Host "Failed:          $failCount" -ForegroundColor $(if ($failCount -gt 0) { 'Red' } else { 'Gray' })
Write-Host "`nReports saved to: $OutputDirectory`n" -ForegroundColor Gray

# Display results table
if ($results.Count -gt 0) {
    $results | Format-Table -AutoSize -Property FileName, Status, DataPoints, @{
        Label = 'Report'
        Expression = { if ($_.ReportPath) { Split-Path $_.ReportPath -Leaf } else { '-' } }
    }
}
