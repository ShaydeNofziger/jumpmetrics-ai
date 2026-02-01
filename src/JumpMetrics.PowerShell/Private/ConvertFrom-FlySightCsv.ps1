function ConvertFrom-FlySightCsv {
    <#
    .SYNOPSIS
        Internal helper to parse FlySight 2 CSV data into structured objects.
    .DESCRIPTION
        Parses FlySight 2 CSV files with the v2 header protocol ($FLYS, $VAR, $COL, $DATA).
        Returns metadata and data points as PowerShell objects.
    .PARAMETER Path
        Path to the FlySight 2 CSV file.
    .OUTPUTS
        PSCustomObject with Metadata and DataPoints properties.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [ValidateScript({ Test-Path $_ -PathType Leaf })]
        [string]$Path
    )

    Write-Verbose "Parsing FlySight 2 CSV file: $Path"
    
    $metadata = @{
        FirmwareVersion = $null
        DeviceId = $null
        SessionId = $null
        FormatVersion = $null
        TotalDataPoints = 0
        RecordingStart = $null
        RecordingEnd = $null
        MaxAltitude = $null
        MinAltitude = $null
    }
    
    $columnMapping = @{}
    $dataPoints = [System.Collections.ArrayList]::new()
    $inDataSection = $false
    
    try {
        $lines = Get-Content -Path $Path -ErrorAction Stop
        
        foreach ($line in $lines) {
            # Skip empty lines
            if ([string]::IsNullOrWhiteSpace($line)) { continue }
            
            # Parse header protocol
            if ($line -match '^\$FLYS,(\d+)$') {
                $metadata.FormatVersion = [int]$matches[1]
                Write-Verbose "FlySight format version: $($metadata.FormatVersion)"
            }
            elseif ($line -match '^\$VAR,([^,]+),(.+)$') {
                $key = $matches[1]
                $value = $matches[2]
                switch ($key) {
                    'FIRMWARE_VER' { $metadata.FirmwareVersion = $value }
                    'DEVICE_ID'    { $metadata.DeviceId = $value }
                    'SESSION_ID'   { $metadata.SessionId = $value }
                }
                Write-Verbose "Metadata: $key = $value"
            }
            elseif ($line -match '^\$COL,GNSS,(.+)$') {
                $columns = $matches[1] -split ','
                for ($i = 0; $i -lt $columns.Length; $i++) {
                    $columnMapping[$columns[$i]] = $i
                }
                Write-Verbose "Column mapping established: $($columnMapping.Keys -join ', ')"
            }
            elseif ($line -eq '$DATA') {
                $inDataSection = $true
                Write-Verbose "Entering data section"
            }
            elseif ($inDataSection -and $line -match '^\$GNSS,(.+)$') {
                # Parse data row
                $values = $matches[1] -split ','
                
                try {
                    $dataPoint = [PSCustomObject]@{
                        Time = [DateTime]::Parse($values[$columnMapping['time']]).ToUniversalTime()
                        Latitude = [double]$values[$columnMapping['lat']]
                        Longitude = [double]$values[$columnMapping['lon']]
                        AltitudeMSL = [double]$values[$columnMapping['hMSL']]
                        VelocityNorth = [double]$values[$columnMapping['velN']]
                        VelocityEast = [double]$values[$columnMapping['velE']]
                        VelocityDown = [double]$values[$columnMapping['velD']]
                        HorizontalAccuracy = [double]$values[$columnMapping['hAcc']]
                        VerticalAccuracy = [double]$values[$columnMapping['vAcc']]
                        SpeedAccuracy = [double]$values[$columnMapping['sAcc']]
                        NumberOfSatellites = [int]$values[$columnMapping['numSV']]
                    }
                    
                    # Add computed properties
                    $dataPoint | Add-Member -NotePropertyName 'HorizontalSpeed' -NotePropertyValue (
                        [Math]::Sqrt([Math]::Pow($dataPoint.VelocityNorth, 2) + [Math]::Pow($dataPoint.VelocityEast, 2))
                    )
                    $dataPoint | Add-Member -NotePropertyName 'VerticalSpeed' -NotePropertyValue (
                        [Math]::Abs($dataPoint.VelocityDown)
                    )
                    
                    [void]$dataPoints.Add($dataPoint)
                }
                catch {
                    Write-Warning "Failed to parse data row: $line. Error: $_"
                }
            }
        }
        
        # Calculate aggregate metadata
        if ($dataPoints.Count -gt 0) {
            $metadata.TotalDataPoints = $dataPoints.Count
            $metadata.RecordingStart = $dataPoints[0].Time
            $metadata.RecordingEnd = $dataPoints[-1].Time
            $metadata.MaxAltitude = ($dataPoints | Measure-Object -Property AltitudeMSL -Maximum).Maximum
            $metadata.MinAltitude = ($dataPoints | Measure-Object -Property AltitudeMSL -Minimum).Minimum
        }
        
        Write-Verbose "Parsed $($dataPoints.Count) data points"
        Write-Verbose "Recording: $($metadata.RecordingStart) to $($metadata.RecordingEnd)"
        Write-Verbose "Altitude range: $($metadata.MinAltitude)m to $($metadata.MaxAltitude)m MSL"
        
        return [PSCustomObject]@{
            Metadata = [PSCustomObject]$metadata
            DataPoints = $dataPoints.ToArray()
        }
    }
    catch {
        Write-Error "Failed to parse FlySight CSV file: $_"
        throw
    }
}
