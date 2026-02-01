function Get-JumpMetrics {
    <#
    .SYNOPSIS
        Displays calculated performance metrics for a jump.
    .DESCRIPTION
        Analyzes FlySight GPS data to calculate and display performance metrics
        including freefall speed, canopy performance, and landing characteristics.
        Can accept either a JumpId (for Azure-stored jumps) or a jump object from Import-FlySightData.
    .PARAMETER JumpId
        The unique identifier of the jump (for Azure-stored jumps).
    .PARAMETER Jump
        A jump object returned from Import-FlySightData.
    .PARAMETER Detailed
        Show detailed segment-by-segment analysis.
    .OUTPUTS
        PSCustomObject containing calculated metrics for freefall, canopy, and landing phases.
    .EXAMPLE
        $jump = Import-FlySightData -Path .\samples\sample-jump.csv
        Get-JumpMetrics -Jump $jump
        
        Calculate and display metrics for a locally parsed jump.
    .EXAMPLE
        Get-JumpMetrics -JumpId 'a1b2c3d4-e5f6-7890-abcd-ef1234567890'
        
        Retrieve metrics for a jump stored in Azure (requires Azure configuration).
    .EXAMPLE
        Import-FlySightData -Path .\samples\sample-jump.csv | Get-JumpMetrics -Detailed
        
        Parse a jump and display detailed metrics via pipeline.
    #>
    [CmdletBinding(DefaultParameterSetName = 'Jump')]
    param(
        [Parameter(Mandatory, ParameterSetName = 'JumpId')]
        [guid]$JumpId,

        [Parameter(Mandatory, ValueFromPipeline, ParameterSetName = 'Jump')]
        [PSCustomObject]$Jump,

        [Parameter()]
        [switch]$Detailed
    )

    process {
        try {
            # Handle JumpId parameter (Azure lookup - not yet implemented)
            if ($PSCmdlet.ParameterSetName -eq 'JumpId') {
                Write-Error "Azure Storage integration not yet implemented. Use -Jump parameter with a parsed jump object."
                return
            }

            # Validate jump object
            if (-not $Jump.DataPoints -or $Jump.DataPoints.Count -eq 0) {
                Write-Error "Jump object has no data points"
                return
            }

            Write-Host "`nAnalyzing jump: $($Jump.FileName)" -ForegroundColor Cyan
            Write-Host ("=" * 60) -ForegroundColor Gray

            # Basic statistics
            $totalDuration = ($Jump.Metadata.RecordingEnd - $Jump.Metadata.RecordingStart).TotalSeconds
            $altitudeRange = $Jump.Metadata.MaxAltitude - $Jump.Metadata.MinAltitude

            # Detect jump phases (simplified heuristic for demo)
            $segments = Get-JumpSegments -DataPoints $Jump.DataPoints -GroundLevel $Jump.Metadata.MinAltitude

            # Calculate metrics for each segment
            $metrics = [PSCustomObject]@{
                JumpId = $Jump.JumpId
                FileName = $Jump.FileName
                Overview = [PSCustomObject]@{
                    TotalDuration = [Math]::Round($totalDuration, 1)
                    TotalDataPoints = $Jump.DataPoints.Count
                    AltitudeRange = [Math]::Round($altitudeRange, 1)
                    GroundLevel = [Math]::Round($Jump.Metadata.MinAltitude, 1)
                    MaxAltitude = [Math]::Round($Jump.Metadata.MaxAltitude, 1)
                }
                Segments = $segments
            }

            # Display overview
            Write-Host "`nOverview:" -ForegroundColor Green
            Write-Host "  Total Duration: $($metrics.Overview.TotalDuration)s" -ForegroundColor White
            Write-Host "  Data Points: $($metrics.Overview.TotalDataPoints)" -ForegroundColor White
            Write-Host "  Altitude Range: $($metrics.Overview.GroundLevel)m - $($metrics.Overview.MaxAltitude)m MSL" -ForegroundColor White
            Write-Host "  Total Altitude Change: $($metrics.Overview.AltitudeRange)m" -ForegroundColor White

            # Display segment analysis
            Write-Host "`nSegment Analysis:" -ForegroundColor Green
            foreach ($segment in $segments) {
                Write-Host "  $($segment.Phase):" -ForegroundColor Yellow
                Write-Host "    Duration: $($segment.Duration)s" -ForegroundColor White
                Write-Host "    Altitude: $($segment.StartAltitude)m → $($segment.EndAltitude)m MSL" -ForegroundColor White
                Write-Host "    Altitude Lost: $($segment.AltitudeLost)m" -ForegroundColor White
                
                if ($segment.Metrics) {
                    foreach ($metric in $segment.Metrics.PSObject.Properties) {
                        Write-Host "    $($metric.Name): $($metric.Value)" -ForegroundColor White
                    }
                }

                if ($Detailed -and $segment.DataPoints.Count -gt 0) {
                    Write-Host "    Data Points: $($segment.DataPoints.Count)" -ForegroundColor Gray
                    Write-Host "    Time Range: $($segment.StartTime.ToString('HH:mm:ss')) - $($segment.EndTime.ToString('HH:mm:ss'))" -ForegroundColor Gray
                }
            }

            Write-Host "`n" + ("=" * 60) -ForegroundColor Gray

            return $metrics
        }
        catch {
            Write-Error "Failed to calculate jump metrics: $_"
            throw
        }
    }
}

# Helper function for segment detection
function Get-JumpSegments {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [System.Collections.Generic.List[PSCustomObject]]$DataPoints,
        
        [Parameter(Mandatory)]
        [double]$GroundLevel
    )

    $segments = @()
    $AGLThreshold = 100 # Altitude above ground level threshold for landing

    # Detect aircraft phase (ascending, velD < 0)
    $aircraftEndIndex = -1
    for ($i = 0; $i -lt $DataPoints.Count - 1; $i++) {
        if ($DataPoints[$i].VelocityDown -ge 0) {
            $aircraftEndIndex = $i
            break
        }
    }

    if ($aircraftEndIndex -gt 10) {
        $aircraftPoints = $DataPoints[0..($aircraftEndIndex - 1)]
        $segments += [PSCustomObject]@{
            Phase = "Aircraft"
            StartTime = $aircraftPoints[0].Time
            EndTime = $aircraftPoints[-1].Time
            Duration = [Math]::Round(($aircraftPoints[-1].Time - $aircraftPoints[0].Time).TotalSeconds, 1)
            StartAltitude = [Math]::Round($aircraftPoints[0].AltitudeMSL, 1)
            EndAltitude = [Math]::Round($aircraftPoints[-1].AltitudeMSL, 1)
            AltitudeLost = [Math]::Round($aircraftPoints[0].AltitudeMSL - $aircraftPoints[-1].AltitudeMSL, 1)
            DataPoints = $aircraftPoints
            Metrics = [PSCustomObject]@{
                "Avg Climb Rate" = "$([Math]::Round(($aircraftPoints | Measure-Object -Property VelocityDown -Average).Average * -1, 1)) m/s"
                "Avg Horizontal Speed" = "$([Math]::Round(($aircraftPoints | Measure-Object -Property HorizontalSpeed -Average).Average, 1)) m/s"
            }
        }
    }

    # Detect exit and freefall (velD > 0 and accelerating/high speed)
    $freefallStartIndex = if ($aircraftEndIndex -gt 0) { $aircraftEndIndex } else { 0 }
    $freefallEndIndex = -1
    $peakVelD = 0

    for ($i = $freefallStartIndex; $i -lt $DataPoints.Count - 5; $i++) {
        $avgVelD = ($DataPoints[$i..$($i+4)] | Measure-Object -Property VelocityDown -Average).Average
        if ($avgVelD -gt $peakVelD) { $peakVelD = $avgVelD }
        
        # Detect deployment (sudden deceleration)
        if ($avgVelD -gt 5 -and $DataPoints[$i].VelocityDown -gt 10) {
            $nextAvg = ($DataPoints[($i+5)..$($i+9)] | Measure-Object -Property VelocityDown -Average).Average
            if ($nextAvg -lt ($avgVelD * 0.5)) { # 50% drop indicates deployment
                $freefallEndIndex = $i
                break
            }
        }
    }

    if ($freefallEndIndex -gt $freefallStartIndex) {
        $freefallPoints = $DataPoints[$freefallStartIndex..($freefallEndIndex - 1)]
        if ($freefallPoints.Count -gt 5) {
            $segments += [PSCustomObject]@{
                Phase = "Freefall"
                StartTime = $freefallPoints[0].Time
                EndTime = $freefallPoints[-1].Time
                Duration = [Math]::Round(($freefallPoints[-1].Time - $freefallPoints[0].Time).TotalSeconds, 1)
                StartAltitude = [Math]::Round($freefallPoints[0].AltitudeMSL, 1)
                EndAltitude = [Math]::Round($freefallPoints[-1].AltitudeMSL, 1)
                AltitudeLost = [Math]::Round($freefallPoints[0].AltitudeMSL - $freefallPoints[-1].AltitudeMSL, 1)
                DataPoints = $freefallPoints
                Metrics = [PSCustomObject]@{
                    "Avg Vertical Speed" = "$([Math]::Round(($freefallPoints | Measure-Object -Property VelocityDown -Average).Average, 1)) m/s"
                    "Max Vertical Speed" = "$([Math]::Round(($freefallPoints | Measure-Object -Property VelocityDown -Maximum).Maximum, 1)) m/s"
                    "Avg Horizontal Speed" = "$([Math]::Round(($freefallPoints | Measure-Object -Property HorizontalSpeed -Average).Average, 1)) m/s"
                }
            }
        }
    }

    # Detect deployment phase (rapid deceleration)
    if ($freefallEndIndex -gt 0) {
        $deploymentStartIndex = $freefallEndIndex
        $deploymentEndIndex = $deploymentStartIndex + 15 # ~3 seconds at 5Hz
        if ($deploymentEndIndex -lt $DataPoints.Count) {
            $deploymentPoints = $DataPoints[$deploymentStartIndex..($deploymentEndIndex - 1)]
            $segments += [PSCustomObject]@{
                Phase = "Deployment"
                StartTime = $deploymentPoints[0].Time
                EndTime = $deploymentPoints[-1].Time
                Duration = [Math]::Round(($deploymentPoints[-1].Time - $deploymentPoints[0].Time).TotalSeconds, 1)
                StartAltitude = [Math]::Round($deploymentPoints[0].AltitudeMSL, 1)
                EndAltitude = [Math]::Round($deploymentPoints[-1].AltitudeMSL, 1)
                AltitudeLost = [Math]::Round($deploymentPoints[0].AltitudeMSL - $deploymentPoints[-1].AltitudeMSL, 1)
                DataPoints = $deploymentPoints
                Metrics = [PSCustomObject]@{
                    "Opening Shock" = "$([Math]::Round($deploymentPoints[0].VelocityDown - $deploymentPoints[-1].VelocityDown, 1)) m/s deceleration"
                }
            }

            # Canopy flight (everything after deployment until near ground)
            $canopyStartIndex = $deploymentEndIndex
            $canopyEndIndex = $DataPoints.Count - 1
            
            # Find where we're approaching ground (last 100m AGL)
            for ($i = $DataPoints.Count - 1; $i -gt $canopyStartIndex; $i--) {
                if (($DataPoints[$i].AltitudeMSL - $GroundLevel) -gt $AGLThreshold) {
                    $canopyEndIndex = $i
                    break
                }
            }

            if ($canopyEndIndex -gt $canopyStartIndex + 10) {
                $canopyPoints = $DataPoints[$canopyStartIndex..$canopyEndIndex]
                $horizontalDistance = 0
                for ($i = 1; $i -lt $canopyPoints.Count; $i++) {
                    $dt = ($canopyPoints[$i].Time - $canopyPoints[$i-1].Time).TotalSeconds
                    $horizontalDistance += $canopyPoints[$i].HorizontalSpeed * $dt
                }
                
                $altitudeLost = $canopyPoints[0].AltitudeMSL - $canopyPoints[-1].AltitudeMSL
                $glideRatio = if ($altitudeLost -gt 0) { $horizontalDistance / $altitudeLost } else { 0 }

                $segments += [PSCustomObject]@{
                    Phase = "Canopy"
                    StartTime = $canopyPoints[0].Time
                    EndTime = $canopyPoints[-1].Time
                    Duration = [Math]::Round(($canopyPoints[-1].Time - $canopyPoints[0].Time).TotalSeconds, 1)
                    StartAltitude = [Math]::Round($canopyPoints[0].AltitudeMSL, 1)
                    EndAltitude = [Math]::Round($canopyPoints[-1].AltitudeMSL, 1)
                    AltitudeLost = [Math]::Round($altitudeLost, 1)
                    DataPoints = $canopyPoints
                    Metrics = [PSCustomObject]@{
                        "Avg Descent Rate" = "$([Math]::Round(($canopyPoints | Measure-Object -Property VelocityDown -Average).Average, 1)) m/s"
                        "Horizontal Distance" = "$([Math]::Round($horizontalDistance, 0)) m"
                        "Glide Ratio" = "$([Math]::Round($glideRatio, 2)):1"
                        "Max Horizontal Speed" = "$([Math]::Round(($canopyPoints | Measure-Object -Property HorizontalSpeed -Maximum).Maximum, 1)) m/s"
                    }
                }
            }

            # Landing (final approach to touchdown)
            if ($canopyEndIndex -lt $DataPoints.Count - 5) {
                $landingPoints = $DataPoints[($canopyEndIndex + 1)..($DataPoints.Count - 1)]
                $segments += [PSCustomObject]@{
                    Phase = "Landing"
                    StartTime = $landingPoints[0].Time
                    EndTime = $landingPoints[-1].Time
                    Duration = [Math]::Round(($landingPoints[-1].Time - $landingPoints[0].Time).TotalSeconds, 1)
                    StartAltitude = [Math]::Round($landingPoints[0].AltitudeMSL, 1)
                    EndAltitude = [Math]::Round($landingPoints[-1].AltitudeMSL, 1)
                    AltitudeLost = [Math]::Round($landingPoints[0].AltitudeMSL - $landingPoints[-1].AltitudeMSL, 1)
                    DataPoints = $landingPoints
                    Metrics = [PSCustomObject]@{
                        "Final Approach Speed" = "$([Math]::Round(($landingPoints | Select-Object -First 10 | Measure-Object -Property HorizontalSpeed -Average).Average, 1)) m/s"
                        "Touchdown Speed" = "$([Math]::Round($landingPoints[-1].HorizontalSpeed, 1)) m/s"
                    }
                }
            }
        }
    }

    return $segments
}
