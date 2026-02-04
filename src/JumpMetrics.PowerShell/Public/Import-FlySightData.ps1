function Import-FlySightData {
    <#
    .SYNOPSIS
        Parses and processes a FlySight 2 CSV file locally.
    .DESCRIPTION
        Reads a FlySight 2 GPS data CSV file and processes it through the complete pipeline:
        parsing, validation, segmentation, and metrics calculation. All processing is done
        locally using the JumpMetrics.Core library.
        
        The processed jump data can optionally be saved to local storage for later retrieval.
    .PARAMETER Path
        Path to the FlySight 2 CSV file.
    .PARAMETER SaveToStorage
        If specified, saves the processed jump data to local storage (~/.jumpmetrics/jumps/).
    .PARAMETER StoragePath
        Custom path for local storage. Defaults to ~/.jumpmetrics/jumps/
    .OUTPUTS
        PSCustomObject with jump processing results including JumpId, Metadata, Segments, and Metrics.
    .EXAMPLE
        Import-FlySightData -Path .\samples\sample-jump.csv
        
        Processes the CSV file locally and displays complete analysis with segments and metrics.
    .EXAMPLE
        Import-FlySightData -Path .\samples\sample-jump.csv -SaveToStorage
        
        Processes the file and saves the results to local storage for later retrieval.
    .EXAMPLE
        Import-FlySightData -Path .\samples\sample-jump.csv -StoragePath "C:\MyJumps"
        
        Processes the file and saves to a custom storage location.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, ValueFromPipeline, ValueFromPipelineByPropertyName)]
        [ValidateScript({ Test-Path $_ -PathType Leaf })]
        [string]$Path,

        [Parameter()]
        [switch]$SaveToStorage,

        [Parameter()]
        [string]$StoragePath = (Join-Path $HOME ".jumpmetrics/jumps")
    )

    begin {
        Write-Verbose "Starting FlySight data import"
    }

    process {
        try {
            $fileName = Split-Path -Path $Path -Leaf
            Write-Host "Processing FlySight file: $fileName" -ForegroundColor Cyan
            
            # Process the jump using local processor
            Write-Verbose "Processing jump with local processor..."
            $jumpData = Invoke-LocalJumpProcessor -Path $Path -Verbose:$VerbosePreference
            
            Write-Host "  ✓ Parsed $($jumpData.Metadata.TotalDataPoints) data points" -ForegroundColor Green
            Write-Host "  ✓ Recording: $($jumpData.Metadata.RecordingStart) to $($jumpData.Metadata.RecordingEnd)" -ForegroundColor Green
            Write-Host "  ✓ Altitude range: $($jumpData.Metadata.MinAltitude.ToString('F1'))m to $($jumpData.Metadata.MaxAltitude.ToString('F1'))m MSL" -ForegroundColor Green
            
            # Display segments
            if ($jumpData.Segments -and $jumpData.Segments.Count -gt 0) {
                Write-Host "`nJump Segments:" -ForegroundColor Cyan
                foreach ($segment in $jumpData.Segments) {
                    $duration = [Math]::Round($segment.Duration, 1)
                    $altLoss = [Math]::Round($segment.StartAltitude - $segment.EndAltitude, 0)
                    Write-Host "  • $($segment.Type): ${duration}s, ${altLoss}m altitude loss" -ForegroundColor Gray
                }
            }
            
            # Display metrics summary
            if ($jumpData.Metrics) {
                Write-Host "`nPerformance Metrics:" -ForegroundColor Cyan
                
                if ($jumpData.Metrics.Freefall) {
                    $ff = $jumpData.Metrics.Freefall
                    Write-Host "  Freefall: $([Math]::Round($ff.TimeInFreefall, 1))s, avg $([Math]::Round($ff.AverageVerticalSpeed, 1)) m/s, max $([Math]::Round($ff.MaxVerticalSpeed, 1)) m/s" -ForegroundColor Gray
                }
                
                if ($jumpData.Metrics.Canopy) {
                    $canopy = $jumpData.Metrics.Canopy
                    Write-Host "  Canopy: $([Math]::Round($canopy.TotalCanopyTime, 1))s, glide ratio $([Math]::Round($canopy.GlideRatio, 2)):1" -ForegroundColor Gray
                }
            }
            
            # Save to local storage if requested
            if ($SaveToStorage) {
                Write-Verbose "Saving jump data to local storage..."
                
                # Ensure storage directory exists
                if (-not (Test-Path $StoragePath)) {
                    New-Item -Path $StoragePath -ItemType Directory -Force | Out-Null
                }
                
                # Convert to JSON and save
                $jsonPath = Join-Path $StoragePath "$($jumpData.JumpId).json"
                $jumpData | ConvertTo-Json -Depth 10 | Out-File -FilePath $jsonPath -Encoding UTF8
                Write-Host "  ✓ Saved to local storage: $jsonPath" -ForegroundColor Green
            }
            
            return $jumpData
        }
        catch {
            Write-Error "Failed to import FlySight data: $_"
            throw
        }
    }

    end {
        Write-Verbose "FlySight data import complete"
    }
}
