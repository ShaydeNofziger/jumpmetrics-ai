<#
.SYNOPSIS
    Example 2: Full Processing with Azure Functions

.DESCRIPTION
    This example demonstrates the complete workflow: uploading a FlySight CSV to
    Azure Functions for full processing including segmentation, metrics calculation,
    and AI analysis.

.NOTES
    Requires:
    - Azure Function App running (local or cloud)
    - Function URL configured

.PARAMETER FunctionUrl
    The URL of your Azure Function API endpoint.
    For local development: http://localhost:7071/api/jumps/analyze
    For production: https://your-function-app.azurewebsites.net/api/jumps/analyze

.PARAMETER FunctionKey
    Optional function key for authentication (required for production deployments).
#>

param(
    [Parameter(Mandatory)]
    [string]$FunctionUrl = "http://localhost:7071/api/jumps/analyze",
    
    [Parameter()]
    [string]$FunctionKey
)

# Import the JumpMetrics module
Import-Module "$PSScriptRoot/../src/JumpMetrics.PowerShell/JumpMetrics.psm1" -Force

Write-Host "`n=== FULL AZURE PROCESSING WORKFLOW ===" -ForegroundColor Cyan
Write-Host "Function URL: $FunctionUrl`n" -ForegroundColor Gray

try {
    # Step 1: Upload and process the jump
    Write-Host "Step 1: Uploading FlySight data to Azure Function..." -ForegroundColor Yellow
    
    $importParams = @{
        Path = "$PSScriptRoot/../samples/sample-jump.csv"
        FunctionUrl = $FunctionUrl
    }
    
    if ($FunctionKey) {
        $importParams['FunctionKey'] = $FunctionKey
    }
    
    $jumpResult = Import-FlySightData @importParams -Verbose
    
    if (-not $jumpResult) {
        Write-Error "Failed to import jump data"
        return
    }
    
    $jumpId = $jumpResult.jumpId
    Write-Host "`n✓ Jump uploaded successfully! Jump ID: $jumpId" -ForegroundColor Green
    
    # Step 2: Display metrics
    if ($jumpResult.metrics) {
        Write-Host "`nStep 2: Displaying performance metrics..." -ForegroundColor Yellow
        Get-JumpMetrics -JumpData $jumpResult
    }
    else {
        Write-Warning "No metrics available in response"
    }
    
    # Step 3: Display AI analysis (if available)
    if ($jumpResult.analysis) {
        Write-Host "`nStep 3: Displaying AI analysis..." -ForegroundColor Yellow
        Get-JumpAnalysis -JumpData $jumpResult
    }
    else {
        Write-Host "`nStep 3: AI analysis not available" -ForegroundColor Yellow
        Write-Host "  Note: AI analysis requires Azure OpenAI configuration" -ForegroundColor Gray
    }
    
    # Step 4: Generate comprehensive report
    Write-Host "`nStep 4: Generating comprehensive report..." -ForegroundColor Yellow
    $reportPath = "$PSScriptRoot/../reports/full-analysis-$jumpId.md"
    Export-JumpReport -JumpData $jumpResult -OutputPath $reportPath
    
    Write-Host "`n" + ("=" * 60) -ForegroundColor Cyan
    Write-Host "✓ WORKFLOW COMPLETE" -ForegroundColor Green
    Write-Host ("=" * 60) -ForegroundColor Cyan
    Write-Host "Jump ID:      $jumpId" -ForegroundColor Gray
    Write-Host "Report:       $reportPath" -ForegroundColor Gray
    Write-Host "`nNext steps:" -ForegroundColor Cyan
    Write-Host "  - View all jumps:    Get-JumpHistory -FunctionUrl '$($FunctionUrl -replace '/api/jumps/analyze', '')'" -ForegroundColor Gray
    Write-Host "  - View this jump:    Get-JumpMetrics -JumpId '$jumpId' -FunctionUrl '...'" -ForegroundColor Gray
    Write-Host ""
}
catch {
    Write-Error "Workflow failed: $_"
    Write-Host "`nTroubleshooting:" -ForegroundColor Yellow
    Write-Host "  1. Ensure Azure Function is running (local: 'func start' in Functions directory)" -ForegroundColor Gray
    Write-Host "  2. Verify Function URL is correct" -ForegroundColor Gray
    Write-Host "  3. Check Function logs for errors" -ForegroundColor Gray
    Write-Host ""
}
