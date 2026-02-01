function Get-JumpHistory {
    <#
    .SYNOPSIS
        Lists all processed jumps.
    .DESCRIPTION
        Retrieves a list of all previously imported and processed jumps.
        When Azure Table Storage is configured, retrieves from cloud storage.
        Otherwise, lists jumps from the local session cache.
    .PARAMETER Count
        Maximum number of jumps to return. Defaults to 20.
    .PARAMETER IncludeLocal
        Include jumps from the current PowerShell session cache.
    .OUTPUTS
        Array of jump summary objects.
    .EXAMPLE
        Get-JumpHistory
        
        List the last 20 processed jumps.
    .EXAMPLE
        Get-JumpHistory -Count 50
        
        List the last 50 processed jumps.
    .EXAMPLE
        Get-JumpHistory -IncludeLocal
        
        List all jumps including those in the current session cache.
    #>
    [CmdletBinding()]
    param(
        [Parameter()]
        [ValidateRange(1, 1000)]
        [int]$Count = 20,

        [Parameter()]
        [switch]$IncludeLocal
    )

    try {
        # Check for Azure Storage configuration
        $hasAzureStorage = $env:AZURE_STORAGE_CONNECTION_STRING

        if (-not $hasAzureStorage) {
            Write-Host "⚠ Azure Storage not configured - showing local session cache only" -ForegroundColor Yellow
            Write-Host "To enable cloud storage, set: `$env:AZURE_STORAGE_CONNECTION_STRING" -ForegroundColor Gray
            Write-Host ""

            # Check for session cache
            if (-not $global:JumpMetricsCache) {
                Write-Host "No jumps in current session. Import a jump with Import-FlySightData to see it here." -ForegroundColor Gray
                return @()
            }

            $jumps = $global:JumpMetricsCache | Select-Object -Last $Count
        }
        else {
            Write-Error "Azure Table Storage integration not yet implemented. Local caching will be added in a future update."
            return @()
        }

        # Display jump history
        Write-Host "`nJump History ($($jumps.Count) jumps):" -ForegroundColor Cyan
        Write-Host ("=" * 80) -ForegroundColor Gray

        if ($jumps.Count -eq 0) {
            Write-Host "No jumps found." -ForegroundColor Gray
            return @()
        }

        $jumps | Format-Table -AutoSize -Property @(
            @{Label='Jump ID'; Expression={$_.JumpId.ToString().Substring(0,8) + '...'}; Width=15},
            @{Label='Date'; Expression={$_.ImportDate.ToString('yyyy-MM-dd HH:mm')}; Width=17},
            @{Label='File'; Expression={$_.FileName}; Width=30},
            @{Label='Duration'; Expression={[Math]::Round(($_.Metadata.RecordingEnd - $_.Metadata.RecordingStart).TotalSeconds, 0).ToString() + 's'}; Width=10},
            @{Label='Max Alt (MSL)'; Expression={[Math]::Round($_.Metadata.MaxAltitude, 0).ToString() + 'm'}; Width=15}
        ) | Out-Host

        # Return jumps array without auto-formatting
        Write-Output $jumps -NoEnumerate
    }
    catch {
        Write-Error "Failed to retrieve jump history: $_"
        throw
    }
}
