#!/usr/bin/env pwsh
# End-to-end test script that processes the sample data through the complete pipeline
# This simulates what the Azure Function would do, but runs locally without needing the Functions host

param(
    [string]$SampleFile = "./samples/sample-jump.csv",
    [string]$OutputReport = "./reports/sample-jump-azure-function-report.md"
)

Write-Host "JumpMetrics AI - Local End-to-End Processing Test" -ForegroundColor Cyan
Write-Host "=" * 60 -ForegroundColor Cyan
Write-Host ""

# Import the PowerShell module
Write-Host "[1/5] Importing PowerShell module..." -ForegroundColor Yellow
Import-Module ./src/JumpMetrics.PowerShell/JumpMetrics.psm1 -Force
Write-Host "✓ Module loaded" -ForegroundColor Green
Write-Host ""

# Load the .NET assembly
Write-Host "[2/5] Loading .NET assemblies..." -ForegroundColor Yellow
$coreDllPath = "./src/JumpMetrics.Core/bin/Release/net10.0/JumpMetrics.Core.dll"
if (-not (Test-Path $coreDllPath)) {
    Write-Host "✗ Core DLL not found. Building project..." -ForegroundColor Yellow
    dotnet build --configuration Release | Out-Null
}
Add-Type -Path $coreDllPath
Write-Host "✓ .NET assemblies loaded" -ForegroundColor Green
Write-Host ""

# Parse the FlySight data
Write-Host "[3/5] Parsing FlySight CSV file..." -ForegroundColor Yellow
$parseResult = ConvertFrom-FlySightCsv -Path $SampleFile
Write-Host "✓ Parsed $($parseResult.DataPoints.Count) data points" -ForegroundColor Green
Write-Host "  Recording: $($parseResult.Metadata.RecordingStart) to $($parseResult.Metadata.RecordingEnd)" -ForegroundColor Gray
Write-Host "  Altitude range: $($parseResult.Metadata.MinAltitude.ToString('F1'))m to $($parseResult.Metadata.MaxAltitude.ToString('F1'))m MSL" -ForegroundColor Gray
Write-Host ""

# Validate the data
Write-Host "[4/5] Validating data..." -ForegroundColor Yellow
$validator = [JumpMetrics.Core.Services.Validation.DataValidator]::new()
$dataPoints = $parseResult.DataPoints
$validationResult = $validator.Validate($dataPoints)

if (-not $validationResult.IsValid) {
    Write-Host "✗ Validation failed!" -ForegroundColor Red
    foreach ($error in $validationResult.Errors) {
        Write-Host "  ERROR: $error" -ForegroundColor Red
    }
    exit 1
}

Write-Host "✓ Data validation passed" -ForegroundColor Green
if ($validationResult.Warnings.Count -gt 0) {
    Write-Host "  Warnings:" -ForegroundColor Yellow
    foreach ($warning in $validationResult.Warnings) {
        Write-Host "    ⚠ $warning" -ForegroundColor Yellow
    }
}
Write-Host ""

# Segment the jump
Write-Host "[5/5] Segmenting jump and calculating metrics..." -ForegroundColor Yellow
$segmenter = [JumpMetrics.Core.Services.Segmentation.JumpSegmenter]::new()
$segments = $segmenter.Segment($dataPoints)
Write-Host "✓ Segmented jump into $($segments.Count) phases" -ForegroundColor Green

# Calculate metrics
$metricsCalculator = [JumpMetrics.Core.Services.Metrics.MetricsCalculator]::new()
$metrics = $metricsCalculator.Calculate($segments)

# Display segments
foreach ($segment in $segments) {
    $duration = [Math]::Round($segment.Duration, 1)
    $altLoss = [Math]::Round($segment.StartAltitude - $segment.EndAltitude, 0)
    Write-Host "  • $($segment.Type): ${duration}s, ${altLoss}m altitude loss" -ForegroundColor Gray
}
Write-Host ""

# Create a jump object for the report
$jump = [PSCustomObject]@{
    jumpId = [Guid]::NewGuid()
    jumpDate = Get-Date
    fileName = Split-Path -Leaf $SampleFile
    flySightFileName = Split-Path -Leaf $SampleFile
    metadata = $parseResult.Metadata
    segments = $segments
    metrics = $metrics
    validationWarnings = $validationResult.Warnings
}

# Generate the report
Write-Host "Generating markdown report..." -ForegroundColor Cyan
$reportPath = Export-JumpReport -JumpData $jump -OutputPath $OutputReport

Write-Host ""
Write-Host "=" * 60 -ForegroundColor Cyan
Write-Host "✓ Processing complete!" -ForegroundColor Green
Write-Host "  Report saved to: $reportPath" -ForegroundColor Cyan
Write-Host "=" * 60 -ForegroundColor Cyan
