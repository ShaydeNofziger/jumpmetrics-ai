function Invoke-LocalJumpProcessor {
    <#
    .SYNOPSIS
        Internal helper to process a jump using the local .NET processor.
    .DESCRIPTION
        Invokes the JumpMetrics.CLI tool to parse, validate, segment, 
        and calculate metrics for a FlySight CSV file, all locally without Azure.
    .PARAMETER Path
        Path to the FlySight 2 CSV file.
    .OUTPUTS
        PSCustomObject with complete jump analysis (metadata, segments, metrics).
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [ValidateScript({ Test-Path $_ -PathType Leaf })]
        [string]$Path
    )

    Write-Verbose "Invoking local jump processor for: $Path"
    
    # Resolve the absolute path to the CLI tool
    # Start from the repository root (go up from Private/ -> PowerShell/ -> src/ -> root/)
    $repoRoot = Split-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) -Parent
    Write-Verbose "Repository root: $repoRoot"
    
    # Try Release build first, then Debug
    $cliTool = Join-Path $repoRoot "src/JumpMetrics.CLI/bin/Release/net10.0/JumpMetrics.CLI.dll"
    if (-not (Test-Path $cliTool)) {
        $cliTool = Join-Path $repoRoot "src/JumpMetrics.CLI/bin/Debug/net10.0/JumpMetrics.CLI.dll"
    }
    
    if (-not (Test-Path $cliTool)) {
        throw "Could not find JumpMetrics.CLI.dll. Please build the project first with 'dotnet build src/JumpMetrics.CLI/JumpMetrics.CLI.csproj --configuration Release'."
    }
    
    Write-Verbose "Using CLI tool from: $cliTool"
    
    try {
        # Execute the CLI tool using dotnet
        Write-Verbose "Processing jump file with CLI..."
        $output = & dotnet $cliTool $Path 2>&1
        
        if ($LASTEXITCODE -ne 0) {
            throw "CLI tool failed with exit code $LASTEXITCODE`: $output"
        }
        
        # Parse JSON output
        $jumpData = $output | ConvertFrom-Json
        Write-Verbose "Successfully processed jump: $($jumpData.jumpId)"
        
        return $jumpData
    }
    catch {
        Write-Error "Failed to process jump with local processor: $_"
        throw
    }
}
