#!/usr/bin/env pwsh
# Batch process multiple FlySight files
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$InputDirectory,
    [Parameter(Mandatory)]
    [string]$OutputDirectory
)

Import-Module ./src/JumpMetrics.PowerShell/JumpMetrics.psm1 -Force

Write-Host "`n=== JumpMetrics AI - Batch Processing ===" -ForegroundColor Cyan

if (-not (Test-Path -Path $InputDirectory -PathType Container)) {
    Write-Error "Input directory not found: $InputDirectory"
    exit 1
}

if (-not (Test-Path -Path $OutputDirectory)) {
    New-Item -Path $OutputDirectory -ItemType Directory -Force | Out-Null
}

$csvFiles = Get-ChildItem -Path $InputDirectory -Filter "*.csv" -File
if ($csvFiles.Count -eq 0) {
    Write-Warning "No CSV files found"
    exit 0
}

Write-Host "Processing $($csvFiles.Count) file(s)...`n" -ForegroundColor Yellow

$successCount = 0
foreach ($file in $csvFiles) {
    Write-Host "Processing: $($file.Name)" -ForegroundColor Cyan
    try {
        $jump = Import-FlySightData -Path $file.FullName
        $metrics = Get-JumpMetrics -Jump $jump
        $analysis = Get-JumpAnalysis -Jump $jump -Metrics $metrics
        $reportPath = Join-Path -Path $OutputDirectory -ChildPath "$($file.BaseName)-report.md"
        Export-JumpReport -Jump $jump -Metrics $metrics -Analysis $analysis -OutputPath $reportPath | Out-Null
        $successCount++
        Write-Host "  ✓ Success`n" -ForegroundColor Green
    }
    catch {
        Write-Host "  ✗ Failed: $_`n" -ForegroundColor Red
    }
}

Write-Host "=== Complete: $successCount/$($csvFiles.Count) processed ===" -ForegroundColor Green
