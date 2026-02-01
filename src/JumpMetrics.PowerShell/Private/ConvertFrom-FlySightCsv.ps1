function ConvertFrom-FlySightCsv {
    <#
    .SYNOPSIS
        Internal helper to parse FlySight 2 CSV data into structured objects.
    .DESCRIPTION
        Parses FlySight 2 CSV files according to the v2 header protocol:
        - $FLYS,<version> - Format version
        - $VAR,<key>,<value> - Device metadata
        - $COL,GNSS,<columns...> - Column definitions (dynamic ordering)
        - $UNIT,GNSS,<units...> - Unit labels
        - $DATA - Marks start of data section
        - $GNSS,<data...> - GPS data rows
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

    Write-Verbose "Parsing FlySight 2 CSV: $Path"
    
    # Initialize result object
    $result = [PSCustomObject]@{
        Metadata = [PSCustomObject]@{
            FormatVersion = $null
            FirmwareVersion = $null
            DeviceId = $null
            SessionId = $null
            TotalDataPoints = 0
            RecordingStart = $null
            RecordingEnd = $null
            MaxAltitude = $null
            MinAltitude = $null
        }
        DataPoints = [System.Collections.Generic.List[PSCustomObject]]::new()
    }

    # Column mapping (filled from $COL line)
    $columnMap = @{}
    $inDataSection = $false

    # Read and parse file
    $lineNumber = 0
    Get-Content -Path $Path | ForEach-Object {
        $lineNumber++
        $line = $_.Trim()

        if ([string]::IsNullOrWhiteSpace($line)) {
            return
        }

        # Parse header protocol
        if ($line -match '^\$FLYS,(\d+)$') {
            $result.Metadata.FormatVersion = [int]$matches[1]
            Write-Verbose "Format version: $($result.Metadata.FormatVersion)"
        }
        elseif ($line -match '^\$VAR,([^,]+),(.*)$') {
            $key = $matches[1]
            $value = $matches[2]
            switch ($key) {
                'FIRMWARE_VER' { $result.Metadata.FirmwareVersion = $value }
                'DEVICE_ID' { $result.Metadata.DeviceId = $value }
                'SESSION_ID' { $result.Metadata.SessionId = $value }
            }
            Write-Verbose "Metadata: $key = $value"
        }
        elseif ($line -match '^\$COL,GNSS,(.+)$') {
            # Parse column names - this defines the field ordering
            $columns = $matches[1] -split ','
            for ($i = 0; $i -lt $columns.Count; $i++) {
                $columnMap[$columns[$i]] = $i
            }
            Write-Verbose "Column mapping: $($columnMap.Keys -join ', ')"
        }
        elseif ($line -match '^\$UNIT,') {
            # Units line - we can validate/store for reference if needed
            Write-Verbose "Units: $line"
        }
        elseif ($line -eq '$DATA') {
            $inDataSection = $true
            Write-Verbose "Entering data section"
        }
        elseif ($inDataSection -and $line -match '^\$GNSS,(.+)$') {
            # Parse GNSS data row
            try {
                $fields = $matches[1] -split ','
                
                # Extract fields using column mapping
                $dataPoint = [PSCustomObject]@{
                    Time = if ($columnMap.ContainsKey('time')) { 
                        [DateTime]::Parse($fields[$columnMap['time']])
                    } else { $null }
                    Latitude = if ($columnMap.ContainsKey('lat')) { 
                        [double]$fields[$columnMap['lat']]
                    } else { 0.0 }
                    Longitude = if ($columnMap.ContainsKey('lon')) { 
                        [double]$fields[$columnMap['lon']]
                    } else { 0.0 }
                    AltitudeMSL = if ($columnMap.ContainsKey('hMSL')) { 
                        [double]$fields[$columnMap['hMSL']]
                    } else { 0.0 }
                    VelocityNorth = if ($columnMap.ContainsKey('velN')) { 
                        [double]$fields[$columnMap['velN']]
                    } else { 0.0 }
                    VelocityEast = if ($columnMap.ContainsKey('velE')) { 
                        [double]$fields[$columnMap['velE']]
                    } else { 0.0 }
                    VelocityDown = if ($columnMap.ContainsKey('velD')) { 
                        [double]$fields[$columnMap['velD']]
                    } else { 0.0 }
                    HorizontalAccuracy = if ($columnMap.ContainsKey('hAcc')) { 
                        [double]$fields[$columnMap['hAcc']]
                    } else { 0.0 }
                    VerticalAccuracy = if ($columnMap.ContainsKey('vAcc')) { 
                        [double]$fields[$columnMap['vAcc']]
                    } else { 0.0 }
                    SpeedAccuracy = if ($columnMap.ContainsKey('sAcc')) { 
                        [double]$fields[$columnMap['sAcc']]
                    } else { 0.0 }
                    NumberOfSatellites = if ($columnMap.ContainsKey('numSV')) { 
                        [int]$fields[$columnMap['numSV']]
                    } else { 0 }
                }

                # Add computed properties
                $dataPoint | Add-Member -MemberType ScriptProperty -Name HorizontalSpeed -Value {
                    [Math]::Sqrt([Math]::Pow($this.VelocityNorth, 2) + [Math]::Pow($this.VelocityEast, 2))
                }
                
                $dataPoint | Add-Member -MemberType ScriptProperty -Name VerticalSpeed -Value {
                    [Math]::Abs($this.VelocityDown)
                }

                $result.DataPoints.Add($dataPoint)
            }
            catch {
                Write-Warning "Failed to parse line ${lineNumber}: ${line} - $_"
            }
        }
    }

    # Calculate metadata statistics
    if ($result.DataPoints.Count -gt 0) {
        $result.Metadata.TotalDataPoints = $result.DataPoints.Count
        $result.Metadata.RecordingStart = $result.DataPoints[0].Time
        $result.Metadata.RecordingEnd = $result.DataPoints[-1].Time
        $result.Metadata.MaxAltitude = ($result.DataPoints | Measure-Object -Property AltitudeMSL -Maximum).Maximum
        $result.Metadata.MinAltitude = ($result.DataPoints | Measure-Object -Property AltitudeMSL -Minimum).Minimum
    }

    Write-Verbose "Parsed $($result.Metadata.TotalDataPoints) data points"
    Write-Verbose "Recording: $($result.Metadata.RecordingStart) to $($result.Metadata.RecordingEnd)"
    Write-Verbose "Altitude range: $($result.Metadata.MinAltitude)m to $($result.Metadata.MaxAltitude)m MSL"

    return $result
}
