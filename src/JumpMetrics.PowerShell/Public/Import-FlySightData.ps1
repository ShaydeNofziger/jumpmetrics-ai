function Import-FlySightData {
    <#
    .SYNOPSIS
        Parses and uploads a FlySight 2 CSV file.
    .DESCRIPTION
        Reads a FlySight 2 GPS data CSV file, validates the data, and uploads it
        to Azure Blob Storage for processing.
    .PARAMETER Path
        Path to the FlySight 2 CSV file.
    .EXAMPLE
        Import-FlySightData -Path .\samples\sample-jump.csv
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, ValueFromPipeline, ValueFromPipelineByPropertyName)]
        [ValidateScript({ Test-Path $_ -PathType Leaf })]
        [string]$Path
    )

    begin {
        Write-Verbose "Starting FlySight data import"
    }

    process {
        throw [System.NotImplementedException]::new("Import-FlySightData is not yet implemented.")
    }

    end {
        Write-Verbose "FlySight data import complete"
    }
}
