function Get-JumpHistory {
    <#
    .SYNOPSIS
        Lists all processed jumps.
    .DESCRIPTION
        Retrieves a list of all previously imported and processed jumps from Azure Table Storage.
        If no Azure Function URL is provided, displays a helpful message about configuring cloud storage.
    .PARAMETER FunctionUrl
        Base URL of the Azure Function API (e.g., "https://jumpmetrics.azurewebsites.net").
        The function will append "/api/jumps" to this URL.
    .PARAMETER FunctionKey
        Function key for authentication (if required by the Azure Function).
    .PARAMETER Count
        Maximum number of jumps to return. Defaults to 20.
    .OUTPUTS
        Array of jump summary objects with JumpId, JumpDate, FileName, and basic metadata.
    .EXAMPLE
        Get-JumpHistory -FunctionUrl "http://localhost:7071" -Count 10
        
        Retrieves the 10 most recent jumps from the local Function API.
    .EXAMPLE
        Get-JumpHistory -FunctionUrl "https://jumpmetrics.azurewebsites.net" -FunctionKey "your-key" | Format-Table
        
        Retrieves jumps from the production API and displays them as a table.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$FunctionUrl,

        [Parameter()]
        [string]$FunctionKey,

        [Parameter()]
        [ValidateRange(1, 1000)]
        [int]$Count = 20
    )

    try {
        Write-Verbose "Retrieving jump history (max $Count jumps)"
        
        # Construct API URL
        $apiUrl = "$($FunctionUrl.TrimEnd('/'))/api/jumps?count=$Count"
        
        # Prepare headers
        $headers = @{}
        if (-not [string]::IsNullOrEmpty($FunctionKey)) {
            $headers['x-functions-key'] = $FunctionKey
        }
        
        try {
            $response = Invoke-RestMethod -Uri $apiUrl -Method Get -Headers $headers -ErrorAction Stop
            
            if ($response.jumps -and $response.jumps.Count -gt 0) {
                $jumps = $response.jumps
                
                Write-Host "`n══════════════════════════════════════════════════════" -ForegroundColor Cyan
                Write-Host "                    JUMP HISTORY" -ForegroundColor Cyan
                Write-Host "══════════════════════════════════════════════════════" -ForegroundColor Cyan
                Write-Host "  Found $($jumps.Count) jump(s)`n" -ForegroundColor Gray
                
                # Display jumps as a table
                $jumps | ForEach-Object {
                    $jumpDate = if ($_.jumpDate) { 
                        [DateTime]::Parse($_.jumpDate).ToString('yyyy-MM-dd HH:mm')
                    } else { 
                        'Unknown' 
                    }
                    
                    [PSCustomObject]@{
                        'Jump ID' = $_.jumpId
                        'Date' = $jumpDate
                        'File Name' = $_.fileName ?? $_.flySightFileName ?? 'Unknown'
                        'Data Points' = $_.metadata.totalDataPoints ?? 'N/A'
                        'Max Altitude' = if ($_.metadata.maxAltitude) { "$([Math]::Round($_.metadata.maxAltitude, 0))m" } else { 'N/A' }
                    }
                } | Format-Table -AutoSize
                
                # Return the jumps array for pipeline usage
                return $jumps
            }
            else {
                Write-Host "`nNo jumps found in storage." -ForegroundColor Yellow
                Write-Host "  → Use Import-FlySightData to process and upload jump files" -ForegroundColor Gray
                return @()
            }
        }
        catch {
            $statusCode = $_.Exception.Response.StatusCode.value__
            if ($statusCode -eq 404) {
                Write-Warning "API endpoint not found. The jumps list endpoint may not be implemented yet."
                Write-Host "  → Current API only supports: POST /api/jumps/analyze" -ForegroundColor Yellow
                Write-Host "     This cmdlet requires: GET /api/jumps" -ForegroundColor Gray
            }
            elseif ($statusCode -eq 401 -or $statusCode -eq 403) {
                Write-Error "Authentication failed. Please provide a valid function key with -FunctionKey."
            }
            else {
                Write-Error "Failed to retrieve jump history: $_"
            }
            return @()
        }
    }
    catch {
        Write-Error "Failed to retrieve jump history: $_"
        throw
    }
}
