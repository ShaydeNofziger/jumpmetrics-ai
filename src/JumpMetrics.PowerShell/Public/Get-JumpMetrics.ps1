function Get-JumpMetrics {
    <#
    .SYNOPSIS
        Displays calculated performance metrics for a jump.
    .DESCRIPTION
        Retrieves and displays freefall, canopy, and landing metrics for a previously
        processed jump. If -FunctionUrl is provided, retrieves metrics from Azure Table Storage.
        If -JumpData is provided, displays metrics from a local jump object.
    .PARAMETER JumpId
        The unique identifier of the jump (retrieved from Azure Storage).
    .PARAMETER FunctionUrl
        Base URL of the Azure Function API (e.g., "https://jumpmetrics.azurewebsites.net").
        The function will append "/api/jumps/{jumpId}/metrics" to this URL.
    .PARAMETER FunctionKey
        Function key for authentication (if required by the Azure Function).
    .PARAMETER JumpData
        A jump object returned from Import-FlySightData (for displaying local metrics).
    .OUTPUTS
        PSCustomObject with Freefall, Canopy, and Landing metrics.
    .EXAMPLE
        Get-JumpMetrics -JumpData $jump
        
        Displays metrics from a jump object returned by Import-FlySightData.
    .EXAMPLE
        Get-JumpMetrics -JumpId 'a1b2c3d4-e5f6-7890-abcd-ef1234567890' -FunctionUrl "http://localhost:7071"
        
        Retrieves metrics from Azure Storage via the local Function API.
    .EXAMPLE
        Import-FlySightData -Path .\jump.csv -FunctionUrl "http://localhost:7071/api/jumps/analyze" | Get-JumpMetrics
        
        Pipeline example: imports a jump and displays its metrics.
    #>
    [CmdletBinding(DefaultParameterSetName = 'FromJumpData')]
    param(
        [Parameter(Mandatory, ParameterSetName = 'FromStorage')]
        [guid]$JumpId,

        [Parameter(Mandatory, ParameterSetName = 'FromStorage')]
        [string]$FunctionUrl,

        [Parameter(ParameterSetName = 'FromStorage')]
        [string]$FunctionKey,

        [Parameter(Mandatory, ParameterSetName = 'FromJumpData', ValueFromPipeline)]
        [PSCustomObject]$JumpData
    )

    process {
        try {
            $metrics = $null

            if ($PSCmdlet.ParameterSetName -eq 'FromStorage') {
                Write-Verbose "Retrieving metrics for Jump ID: $JumpId"
                
                # Construct API URL
                $apiUrl = "$($FunctionUrl.TrimEnd('/'))/api/jumps/$JumpId/metrics"
                
                # Prepare headers
                $headers = @{}
                if (-not [string]::IsNullOrEmpty($FunctionKey)) {
                    $headers['x-functions-key'] = $FunctionKey
                }
                
                try {
                    $response = Invoke-RestMethod -Uri $apiUrl -Method Get -Headers $headers -ErrorAction Stop
                    $metrics = $response.metrics
                }
                catch {
                    Write-Error "Failed to retrieve metrics from Azure: $_"
                    return
                }
            }
            else {
                # Extract metrics from JumpData object
                if ($JumpData.PSObject.Properties['metrics']) {
                    $metrics = $JumpData.metrics
                }
                elseif ($JumpData.PSObject.Properties['Metrics']) {
                    $metrics = $JumpData.Metrics
                }
                else {
                    Write-Warning "No metrics found in jump data. The jump may not have been fully processed."
                    return
                }
            }

            if ($null -eq $metrics) {
                Write-Warning "No metrics available for this jump."
                return
            }

            # Display metrics in a formatted way
            Write-Host "`n══════════════════════════════════════════════════════" -ForegroundColor Cyan
            Write-Host "                 JUMP PERFORMANCE METRICS" -ForegroundColor Cyan
            Write-Host "══════════════════════════════════════════════════════" -ForegroundColor Cyan

            # Freefall Metrics
            if ($metrics.freefall -or $metrics.Freefall) {
                $freefall = if ($metrics.freefall) { $metrics.freefall } else { $metrics.Freefall }
                Write-Host "`n📉 FREEFALL METRICS" -ForegroundColor Yellow
                Write-Host "  Time in Freefall:        $([Math]::Round($freefall.timeInFreefall ?? $freefall.TimeInFreefall, 1)) seconds" -ForegroundColor Gray
                Write-Host "  Avg Vertical Speed:      $([Math]::Round($freefall.averageVerticalSpeed ?? $freefall.AverageVerticalSpeed, 1)) m/s" -ForegroundColor Gray
                Write-Host "  Max Vertical Speed:      $([Math]::Round($freefall.maxVerticalSpeed ?? $freefall.MaxVerticalSpeed, 1)) m/s" -ForegroundColor Gray
                Write-Host "  Avg Horizontal Speed:    $([Math]::Round($freefall.averageHorizontalSpeed ?? $freefall.AverageHorizontalSpeed, 1)) m/s" -ForegroundColor Gray
                
                if ($null -ne ($freefall.trackAngle ?? $freefall.TrackAngle)) {
                    Write-Host "  Track Angle:             $([Math]::Round($freefall.trackAngle ?? $freefall.TrackAngle, 1))°" -ForegroundColor Gray
                }
            }
            else {
                Write-Host "`n📉 FREEFALL METRICS" -ForegroundColor Yellow
                Write-Host "  No freefall detected (possible high pull or hop-n-pop)" -ForegroundColor Gray
            }

            # Canopy Metrics
            if ($metrics.canopy -or $metrics.Canopy) {
                $canopy = if ($metrics.canopy) { $metrics.canopy } else { $metrics.Canopy }
                Write-Host "`n🪂 CANOPY METRICS" -ForegroundColor Green
                Write-Host "  Deployment Altitude:     $([Math]::Round($canopy.deploymentAltitude ?? $canopy.DeploymentAltitude, 0)) m MSL" -ForegroundColor Gray
                Write-Host "  Avg Descent Rate:        $([Math]::Round($canopy.averageDescentRate ?? $canopy.AverageDescentRate, 1)) m/s" -ForegroundColor Gray
                Write-Host "  Glide Ratio:             $([Math]::Round($canopy.glideRatio ?? $canopy.GlideRatio, 2)):1" -ForegroundColor Gray
                Write-Host "  Max Horizontal Speed:    $([Math]::Round($canopy.maxHorizontalSpeed ?? $canopy.MaxHorizontalSpeed, 1)) m/s" -ForegroundColor Gray
                Write-Host "  Total Canopy Time:       $([Math]::Round($canopy.totalCanopyTime ?? $canopy.TotalCanopyTime, 1)) seconds" -ForegroundColor Gray
                
                if ($null -ne ($canopy.patternAltitude ?? $canopy.PatternAltitude)) {
                    Write-Host "  Pattern Entry Altitude:  $([Math]::Round($canopy.patternAltitude ?? $canopy.PatternAltitude, 0)) m MSL" -ForegroundColor Gray
                }
            }
            else {
                Write-Host "`n🪂 CANOPY METRICS" -ForegroundColor Green
                Write-Host "  No canopy flight detected" -ForegroundColor Gray
            }

            # Landing Metrics
            if ($metrics.landing -or $metrics.Landing) {
                $landing = if ($metrics.landing) { $metrics.landing } else { $metrics.Landing }
                Write-Host "`n🛬 LANDING METRICS" -ForegroundColor Blue
                Write-Host "  Final Approach Speed:    $([Math]::Round($landing.finalApproachSpeed ?? $landing.FinalApproachSpeed, 1)) m/s" -ForegroundColor Gray
                Write-Host "  Touchdown Vertical Speed: $([Math]::Round($landing.touchdownVerticalSpeed ?? $landing.TouchdownVerticalSpeed, 1)) m/s" -ForegroundColor Gray
                
                if ($null -ne ($landing.landingAccuracy ?? $landing.LandingAccuracy)) {
                    Write-Host "  Landing Accuracy:        $([Math]::Round($landing.landingAccuracy ?? $landing.LandingAccuracy, 1)) m" -ForegroundColor Gray
                }
            }
            else {
                Write-Host "`n🛬 LANDING METRICS" -ForegroundColor Blue
                Write-Host "  No landing detected" -ForegroundColor Gray
            }

            Write-Host "`n══════════════════════════════════════════════════════`n" -ForegroundColor Cyan

            # Return the metrics object for pipeline usage
            return $metrics
        }
        catch {
            Write-Error "Failed to retrieve jump metrics: $_"
            throw
        }
    }
}
