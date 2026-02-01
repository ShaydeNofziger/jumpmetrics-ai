#!/usr/bin/env pwsh
# PowerShell pipeline usage examples
Import-Module ./src/JumpMetrics.PowerShell/JumpMetrics.psm1 -Force

Write-Host "`n=== JumpMetrics AI - Pipeline Examples ===" -ForegroundColor Cyan

Write-Host "`n1. Import → Metrics pipeline:" -ForegroundColor Yellow
Import-FlySightData -Path ./samples/sample-jump.csv | Get-JumpMetrics | Out-Null

Write-Host "`n2. Full pipeline with variables:" -ForegroundColor Yellow
$jump = Import-FlySightData -Path ./samples/sample-jump.csv
$metrics = $jump | Get-JumpMetrics
$analysis = Get-JumpAnalysis -Jump $jump -Metrics $metrics

Write-Host "`n3. Jump history filtering:" -ForegroundColor Yellow
$history = Get-JumpHistory
$highJumps = $history | Where-Object { $_.Metadata.MaxAltitude -gt 1500 }
Write-Host "Found $($highJumps.Count) jumps above 1500m" -ForegroundColor White

Write-Host "`n=== Pipeline Examples Complete ===" -ForegroundColor Green
