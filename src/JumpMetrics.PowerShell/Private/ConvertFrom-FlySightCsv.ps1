function ConvertFrom-FlySightCsv {
    <#
    .SYNOPSIS
        Internal helper to parse FlySight 2 CSV data into structured objects.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Path
    )

    throw [System.NotImplementedException]::new("ConvertFrom-FlySightCsv is not yet implemented.")
}
