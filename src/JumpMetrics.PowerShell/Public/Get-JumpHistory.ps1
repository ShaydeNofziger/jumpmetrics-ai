function Get-JumpHistory {
    <#
    .SYNOPSIS
        Lists all processed jumps from local storage.
    .DESCRIPTION
        Retrieves a list of all previously imported and processed jumps from local storage.
    .PARAMETER StoragePath
        Path to the local storage directory. Defaults to ~/.jumpmetrics/jumps/
    .PARAMETER Count
        Maximum number of jumps to return. Defaults to 20.
    .OUTPUTS
        Array of jump summary objects with JumpId, JumpDate, FileName, and basic metadata.
    .EXAMPLE
        Get-JumpHistory
        
        Retrieves jumps from the default local storage location.
    .EXAMPLE
        Get-JumpHistory -StoragePath "C:\MyJumps" -Count 10 | Format-Table
        
        Retrieves the 10 most recent jumps from a custom storage location.
    #>
    [CmdletBinding()]
    param(
        [Parameter()]
        [string]$StoragePath = (Join-Path $HOME ".jumpmetrics/jumps"),

        [Parameter()]
        [ValidateRange(1, 1000)]
        [int]$Count = 20
    )

    # Helper function to format altitude
    function Format-Altitude {
        param($altitude)
        if ($null -ne $altitude) {
            "$([Math]::Round($altitude, 0))m"
        } else {
            'N/A'
        }
    }

    try {
        Write-Verbose "Retrieving jump history from: $StoragePath"
        
        if (-not (Test-Path $StoragePath)) {
            Write-Host "`nNo jumps found in storage." -ForegroundColor Yellow
            Write-Host "  Storage path does not exist: $StoragePath" -ForegroundColor Gray
            Write-Host "  → Use Import-FlySightData -SaveToStorage to process and save jump files" -ForegroundColor Gray
            return @()
        }
        
        # Get all JSON files from storage
        $jsonFiles = Get-ChildItem -Path $StoragePath -Filter "*.json" | 
                     Sort-Object LastWriteTime -Descending | 
                     Select-Object -First $Count
        
        if ($jsonFiles.Count -eq 0) {
            Write-Host "`nNo jumps found in storage." -ForegroundColor Yellow
            Write-Host "  → Use Import-FlySightData -SaveToStorage to process and save jump files" -ForegroundColor Gray
            return @()
        }
        
        Write-Host "`n══════════════════════════════════════════════════════" -ForegroundColor Cyan
        Write-Host "                    JUMP HISTORY" -ForegroundColor Cyan
        Write-Host "══════════════════════════════════════════════════════" -ForegroundColor Cyan
        Write-Host "  Found $($jsonFiles.Count) jump(s)`n" -ForegroundColor Gray
        
        # Load and display jumps
        $jumps = @()
        $displayData = @()
        
        foreach ($file in $jsonFiles) {
            try {
                $jump = Get-Content -Path $file.FullName -Raw | ConvertFrom-Json
                $jumps += $jump
                
                $jumpDate = if ($jump.jumpDate) { 
                    [DateTime]::Parse($jump.jumpDate).ToString('yyyy-MM-dd HH:mm')
                } else { 
                    'Unknown' 
                }
                
                $displayData += [PSCustomObject]@{
                    'Jump ID' = $jump.jumpId
                    'Date' = $jumpDate
                    'File Name' = $jump.flySightFileName ?? 'Unknown'
                    'Data Points' = $jump.metadata.totalDataPoints ?? 'N/A'
                    'Max Altitude' = Format-Altitude $jump.metadata.maxAltitude
                }
            }
            catch {
                Write-Warning "Failed to load jump from $($file.Name): $_"
            }
        }
        
        $displayData | Format-Table -AutoSize
        
        # Return the jumps array for pipeline usage
        return $jumps
    }
    catch {
        Write-Error "Failed to retrieve jump history: $_"
        throw
    }
}
