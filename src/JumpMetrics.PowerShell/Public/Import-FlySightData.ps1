function Import-FlySightData {
    <#
    .SYNOPSIS
        Parses a FlySight 2 CSV file and returns structured jump data.
    .DESCRIPTION
        Reads a FlySight 2 GPS data CSV file, parses the v2 header protocol,
        validates the data, and returns a structured object containing metadata
        and data points. Optionally uploads to Azure Blob Storage if configured.
    .PARAMETER Path
        Path to the FlySight 2 CSV file.
    .PARAMETER PassThru
        Return the parsed jump object even when uploading to Azure.
    .OUTPUTS
        PSCustomObject containing JumpId, FileName, Metadata, DataPoints, and ValidationResults.
    .EXAMPLE
        Import-FlySightData -Path .\samples\sample-jump.csv
        
        Parses the FlySight CSV and returns structured jump data.
    .EXAMPLE
        $jump = Import-FlySightData -Path .\samples\sample-jump.csv -PassThru
        
        Parse and store the result in a variable for further processing.
    .EXAMPLE
        Get-ChildItem *.csv | Import-FlySightData
        
        Process multiple FlySight files from the pipeline.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, ValueFromPipeline, ValueFromPipelineByPropertyName)]
        [ValidateScript({ Test-Path $_ -PathType Leaf })]
        [string]$Path,

        [Parameter()]
        [switch]$PassThru
    )

    begin {
        Write-Verbose "Starting FlySight data import"
    }

    process {
        try {
            # Resolve to absolute path
            $resolvedPath = Resolve-Path -Path $Path -ErrorAction Stop
            $fileName = Split-Path -Path $resolvedPath -Leaf

            Write-Host "Importing FlySight data from: $fileName" -ForegroundColor Cyan

            # Parse the CSV file
            $parsed = ConvertFrom-FlySightCsv -Path $resolvedPath -Verbose:$VerbosePreference

            # Validate data quality
            $validation = @{
                Errors = @()
                Warnings = @()
                IsValid = $true
            }

            # Check for sufficient data
            if ($parsed.DataPoints.Count -lt 10) {
                $validation.Errors += "Insufficient data points: $($parsed.DataPoints.Count) (minimum 10 required)"
                $validation.IsValid = $false
            }

            # Check for GPS acquisition issues (high accuracy errors, low satellite count)
            $poorAccuracyPoints = $parsed.DataPoints | Where-Object { $_.HorizontalAccuracy -gt 50 }
            if ($poorAccuracyPoints.Count -gt 0) {
                $validation.Warnings += "Found $($poorAccuracyPoints.Count) data points with poor GPS accuracy (hAcc > 50m)"
            }

            $lowSatellitePoints = $parsed.DataPoints | Where-Object { $_.NumberOfSatellites -lt 6 }
            if ($lowSatellitePoints.Count -gt 0) {
                $validation.Warnings += "Found $($lowSatellitePoints.Count) data points with insufficient satellites (< 6)"
            }

            # Check for time gaps
            for ($i = 1; $i -lt $parsed.DataPoints.Count; $i++) {
                $gap = ($parsed.DataPoints[$i].Time - $parsed.DataPoints[$i-1].Time).TotalSeconds
                if ($gap -gt 2) {
                    $validation.Warnings += "Time gap detected: $([Math]::Round($gap, 1))s between data points $i and $($i+1)"
                }
            }

            # Create jump object
            $jumpId = [guid]::NewGuid()
            $jump = [PSCustomObject]@{
                JumpId = $jumpId
                FileName = $fileName
                ImportDate = Get-Date
                FilePath = $resolvedPath.ToString()
                Metadata = $parsed.Metadata
                DataPoints = $parsed.DataPoints
                ValidationResults = $validation
            }

            # Display summary
            Write-Host "✓ Successfully parsed $($parsed.DataPoints.Count) data points" -ForegroundColor Green
            Write-Host "  Jump ID: $jumpId" -ForegroundColor Gray
            Write-Host "  Recording: $($parsed.Metadata.RecordingStart.ToString('yyyy-MM-dd HH:mm:ss')) to $($parsed.Metadata.RecordingEnd.ToString('HH:mm:ss'))" -ForegroundColor Gray
            Write-Host "  Altitude: $([Math]::Round($parsed.Metadata.MinAltitude, 1))m to $([Math]::Round($parsed.Metadata.MaxAltitude, 1))m MSL" -ForegroundColor Gray
            Write-Host "  Duration: $([Math]::Round(($parsed.Metadata.RecordingEnd - $parsed.Metadata.RecordingStart).TotalSeconds, 1))s" -ForegroundColor Gray

            if ($validation.Errors.Count -gt 0) {
                Write-Host "  Validation Errors:" -ForegroundColor Red
                $validation.Errors | ForEach-Object { Write-Host "    - $_" -ForegroundColor Red }
            }

            if ($validation.Warnings.Count -gt 0) {
                Write-Host "  Validation Warnings:" -ForegroundColor Yellow
                $validation.Warnings | ForEach-Object { Write-Host "    - $_" -ForegroundColor Yellow }
            }

            # Check for Azure Storage connection (optional)
            if ($env:AZURE_STORAGE_CONNECTION_STRING) {
                Write-Verbose "Azure Storage connection string detected - upload capability available"
                Write-Host "  Note: Azure upload not yet implemented (local processing only)" -ForegroundColor Yellow
            }

            # Cache jump in session for Get-JumpHistory
            if (-not $global:JumpMetricsCache) {
                $global:JumpMetricsCache = [System.Collections.Generic.List[PSCustomObject]]::new()
            }
            $global:JumpMetricsCache.Add($jump)
            Write-Verbose "Jump cached in session (accessible via Get-JumpHistory)"

            # Return the jump object
            if ($PassThru -or -not $env:AZURE_STORAGE_CONNECTION_STRING) {
                return $jump
            }
        }
        catch {
            Write-Error "Failed to import FlySight data from ${Path}: $_"
            throw
        }
    }

    end {
        Write-Verbose "FlySight data import complete"
    }
}
