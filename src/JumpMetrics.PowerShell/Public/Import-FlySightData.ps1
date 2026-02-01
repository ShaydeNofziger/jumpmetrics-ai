function Import-FlySightData {
    <#
    .SYNOPSIS
        Parses and uploads a FlySight 2 CSV file for processing.
    .DESCRIPTION
        Reads a FlySight 2 GPS data CSV file, validates the data locally, and optionally
        uploads it to the Azure Function API for full processing (segmentation, metrics, AI analysis).
        
        If -LocalOnly is specified, performs local parsing and validation without uploading.
        If -FunctionUrl is not provided, only local processing is performed.
    .PARAMETER Path
        Path to the FlySight 2 CSV file.
    .PARAMETER FunctionUrl
        URL of the Azure Function API endpoint (e.g., "https://jumpmetrics.azurewebsites.net/api/jumps/analyze").
        If not provided, only local parsing is performed.
    .PARAMETER FunctionKey
        Function key for authentication (if required by the Azure Function).
    .PARAMETER LocalOnly
        If specified, only performs local parsing without uploading to Azure.
    .OUTPUTS
        PSCustomObject with jump processing results including JumpId, Metadata, Segments, and Metrics.
    .EXAMPLE
        Import-FlySightData -Path .\samples\sample-jump.csv -LocalOnly
        
        Parses the CSV file locally and displays metadata and basic validation results.
    .EXAMPLE
        Import-FlySightData -Path .\samples\sample-jump.csv -FunctionUrl "http://localhost:7071/api/jumps/analyze"
        
        Uploads the CSV file to the local Azure Function for full processing.
    .EXAMPLE
        Import-FlySightData -Path .\samples\sample-jump.csv -FunctionUrl "https://jumpmetrics.azurewebsites.net/api/jumps/analyze" -FunctionKey "your-key"
        
        Uploads the CSV file to the production Azure Function with authentication.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, ValueFromPipeline, ValueFromPipelineByPropertyName)]
        [ValidateScript({ Test-Path $_ -PathType Leaf })]
        [string]$Path,

        [Parameter()]
        [string]$FunctionUrl,

        [Parameter()]
        [string]$FunctionKey,

        [Parameter()]
        [switch]$LocalOnly
    )

    begin {
        Write-Verbose "Starting FlySight data import"
    }

    process {
        try {
            $fileName = Split-Path -Path $Path -Leaf
            Write-Host "Processing FlySight file: $fileName" -ForegroundColor Cyan
            
            # Step 1: Parse locally
            Write-Verbose "Parsing FlySight CSV file locally..."
            $parseResult = ConvertFrom-FlySightCsv -Path $Path -Verbose:$VerbosePreference
            
            if ($parseResult.DataPoints.Count -eq 0) {
                Write-Error "No data points found in file. File may be corrupted or empty."
                return
            }
            
            Write-Host "  ✓ Parsed $($parseResult.DataPoints.Count) data points" -ForegroundColor Green
            Write-Host "  ✓ Recording: $($parseResult.Metadata.RecordingStart) to $($parseResult.Metadata.RecordingEnd)" -ForegroundColor Green
            Write-Host "  ✓ Altitude range: $($parseResult.Metadata.MinAltitude.ToString('F1'))m to $($parseResult.Metadata.MaxAltitude.ToString('F1'))m MSL" -ForegroundColor Green
            
            # Step 2: Basic validation
            if ($parseResult.DataPoints.Count -lt 10) {
                Write-Warning "File contains fewer than 10 data points - insufficient for jump analysis"
            }
            
            $poorAccuracyPoints = @($parseResult.DataPoints | Where-Object { $_.HorizontalAccuracy -gt 50 })
            if ($poorAccuracyPoints.Count -gt 0) {
                Write-Warning "Found $($poorAccuracyPoints.Count) data points with poor GPS accuracy (>50m)"
            }
            
            # Step 3: Upload to Azure Function (if not LocalOnly and FunctionUrl provided)
            if (-not $LocalOnly -and -not [string]::IsNullOrEmpty($FunctionUrl)) {
                Write-Verbose "Uploading to Azure Function: $FunctionUrl"
                Write-Host "  → Uploading to Azure Function for full processing..." -ForegroundColor Yellow
                
                # Read file content as bytes for upload
                $fileContent = [System.IO.File]::ReadAllBytes((Resolve-Path $Path))
                
                # Prepare headers
                $headers = @{
                    'X-FileName' = $fileName
                    'Content-Type' = 'text/csv'
                }
                
                if (-not [string]::IsNullOrEmpty($FunctionKey)) {
                    $headers['x-functions-key'] = $FunctionKey
                }
                
                # Upload to Function API
                try {
                    $response = Invoke-RestMethod -Uri $FunctionUrl -Method Post -Body $fileContent -Headers $headers -ErrorAction Stop
                    
                    Write-Host "  ✓ Upload successful! Jump ID: $($response.jumpId)" -ForegroundColor Green
                    
                    # Display segments
                    if ($response.segments -and $response.segments.Count -gt 0) {
                        Write-Host "`nJump Segments:" -ForegroundColor Cyan
                        foreach ($segment in $response.segments) {
                            $duration = [Math]::Round($segment.duration, 1)
                            $altLoss = [Math]::Round($segment.startAltitude - $segment.endAltitude, 0)
                            Write-Host "  • $($segment.type): ${duration}s, ${altLoss}m altitude loss" -ForegroundColor Gray
                        }
                    }
                    
                    # Display validation warnings
                    if ($response.validationWarnings -and $response.validationWarnings.Count -gt 0) {
                        Write-Host "`nValidation Warnings:" -ForegroundColor Yellow
                        foreach ($warning in $response.validationWarnings) {
                            Write-Host "  ⚠ $warning" -ForegroundColor Yellow
                        }
                    }
                    
                    return $response
                }
                catch {
                    Write-Error "Failed to upload to Azure Function: $_"
                    Write-Host "  → Returning local parse results only" -ForegroundColor Yellow
                    return $parseResult
                }
            }
            else {
                if ($LocalOnly) {
                    Write-Host "  → Local parsing only (use -FunctionUrl to upload for full processing)" -ForegroundColor Yellow
                }
                elseif ([string]::IsNullOrEmpty($FunctionUrl)) {
                    Write-Host "  → No FunctionUrl provided - returning local parse results" -ForegroundColor Yellow
                    Write-Host "     Tip: Use -FunctionUrl to upload for segmentation, metrics, and AI analysis" -ForegroundColor Gray
                }
                
                return $parseResult
            }
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
