function Get-JumpHistory {
    <#
    .SYNOPSIS
        Lists all processed jumps.
    .DESCRIPTION
        Retrieves a list of all previously imported and processed jumps
        from Azure Table Storage.
    .PARAMETER Count
        Maximum number of jumps to return. Defaults to 20.
    .EXAMPLE
        Get-JumpHistory
    .EXAMPLE
        Get-JumpHistory -Count 50
    #>
    [CmdletBinding()]
    param(
        [ValidateRange(1, 1000)]
        [int]$Count = 20
    )

    throw [System.NotImplementedException]::new("Get-JumpHistory is not yet implemented.")
}
