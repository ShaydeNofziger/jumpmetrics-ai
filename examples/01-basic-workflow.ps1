#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Basic workflow: Import a FlySight file, calculate metrics, and view analysis.

.DESCRIPTION
    This example demonstrates the core workflow of the JumpMetrics PowerShell module:
    1. Import a FlySight 2 CSV file
    2. Calculate and display performance metrics
    3. Generate AI-powered analysis (mock if Azure not configured)
    4. List jump history

.EXAMPLE
    ./examples/01-basic-workflow.ps1

.NOTES
    Requires PowerShell 7.5+ and the JumpMetrics module.
#>

# Import the JumpMetrics module
Import-Module ./src/JumpMetrics.PowerShell/JumpMetrics.psm1 -Force

Write-Host "`n=== JumpMetrics AI - Basic Workflow Example ===" -ForegroundColor Cyan
Write-Host "This example demonstrates importing and analyzing a FlySight 2 GPS file.`n" -ForegroundColor Gray

# Step 1: Import FlySight data
Write-Host "Step 1: Importing FlySight GPS data..." -ForegroundColor Yellow
$jump = Import-FlySightData -Path ./samples/sample-jump.csv

Write-Host "`nPress Enter to continue to metrics calculation..." -ForegroundColor Gray
Read-Host

# Step 2: Calculate metrics
Write-Host "`nStep 2: Calculating performance metrics..." -ForegroundColor Yellow
$metrics = Get-JumpMetrics -Jump $jump

Write-Host "`nPress Enter to continue to AI analysis..." -ForegroundColor Gray
Read-Host

# Step 3: Get AI analysis
Write-Host "`nStep 3: Generating AI analysis..." -ForegroundColor Yellow
$analysis = Get-JumpAnalysis -Jump $jump -Metrics $metrics

Write-Host "`nPress Enter to view jump history..." -ForegroundColor Gray
Read-Host

# Step 4: View jump history
Write-Host "`nStep 4: Viewing jump history..." -ForegroundColor Yellow
Get-JumpHistory | Out-Null

Write-Host "`n=== Workflow Complete ===" -ForegroundColor Green
Write-Host "You can now export a report with:" -ForegroundColor Gray
Write-Host "  Export-JumpReport -Jump `$jump -Metrics `$metrics -Analysis `$analysis -OutputPath ./my-jump-report.md" -ForegroundColor Gray
