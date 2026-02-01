# Contributing to JumpMetrics AI

Thank you for your interest in contributing to JumpMetrics AI! This document provides guidelines for contributing to the project, with a focus on PowerShell development best practices.

## Table of Contents

- [Getting Started](#getting-started)
- [Development Setup](#development-setup)
- [PowerShell Best Practices](#powershell-best-practices)
- [Testing](#testing)
- [Pull Request Process](#pull-request-process)
- [Code Style](#code-style)

## Getting Started

1. Fork the repository
2. Clone your fork: `git clone https://github.com/YOUR-USERNAME/jumpmetrics-ai.git`
3. Create a feature branch: `git checkout -b feature/your-feature-name`
4. Make your changes
5. Test your changes
6. Commit and push: `git push origin feature/your-feature-name`
7. Open a Pull Request

## Development Setup

### Prerequisites

- PowerShell 7.5 or higher
- .NET 10 SDK
- Git
- Pester (PowerShell testing framework)

### Install Development Tools

```powershell
# Install Pester for testing
Install-Module -Name Pester -Force -SkipPublisherCheck

# Verify PowerShell version
$PSVersionTable.PSVersion  # Should be 7.5+

# Verify .NET SDK
dotnet --version  # Should be 10.x
```

### Project Structure

```
src/JumpMetrics.PowerShell/
├── JumpMetrics.psd1          # Module manifest
├── JumpMetrics.psm1          # Module loader
├── Public/                   # Exported cmdlets
│   ├── Import-FlySightData.ps1
│   ├── Get-JumpMetrics.ps1
│   ├── Get-JumpAnalysis.ps1
│   ├── Get-JumpHistory.ps1
│   └── Export-JumpReport.ps1
├── Private/                  # Internal functions
│   └── ConvertFrom-FlySightCsv.ps1
└── Tests/                    # Pester tests
    └── JumpMetrics.Tests.ps1
```

## PowerShell Best Practices

### Cmdlet Naming

Follow PowerShell's Verb-Noun naming convention:

```powershell
# Good
Get-JumpMetrics
Import-FlySightData
Export-JumpReport

# Bad
GetMetrics
ImportData
ReportExport
```

Approved verbs: `Get`, `Set`, `New`, `Remove`, `Start`, `Stop`, `Import`, `Export`, `Test`, `Clear`, etc.

Use `Get-Verb` to see all approved verbs.

### Comment-Based Help

All public functions must include comprehensive comment-based help:

```powershell
function Get-JumpMetrics {
    <#
    .SYNOPSIS
        Brief one-line description.
    
    .DESCRIPTION
        Detailed description of what the function does.
    
    .PARAMETER JumpId
        Description of the parameter.
    
    .EXAMPLE
        Get-JumpMetrics -JumpId 'abc123'
        
        Description of what this example does.
    
    .EXAMPLE
        Import-FlySightData -Path ./data.csv | Get-JumpMetrics
        
        Pipeline example.
    
    .OUTPUTS
        PSCustomObject containing metrics data.
    
    .NOTES
        Additional information, caveats, or requirements.
    #>
}
```

### Parameter Validation

Use PowerShell's built-in validation attributes:

```powershell
param(
    # Required parameter
    [Parameter(Mandatory)]
    [string]$Path,
    
    # Validate file exists
    [Parameter(Mandatory)]
    [ValidateScript({ Test-Path $_ -PathType Leaf })]
    [string]$InputFile,
    
    # Validate range
    [ValidateRange(1, 1000)]
    [int]$Count = 20,
    
    # Validate set
    [ValidateSet('Low', 'Medium', 'High')]
    [string]$Priority,
    
    # Pipeline support
    [Parameter(ValueFromPipeline, ValueFromPipelineByPropertyName)]
    [PSCustomObject]$Jump
)
```

### Error Handling

Use structured error handling with appropriate error actions:

```powershell
try {
    # Risky operation
    $data = Import-Csv -Path $Path -ErrorAction Stop
}
catch [System.IO.FileNotFoundException] {
    Write-Error "File not found: $Path"
    return
}
catch {
    Write-Error "Failed to import data: $_"
    throw
}
```

### Output and Formatting

- Use `Write-Host` for UI messages (colored output, progress)
- Use `Write-Verbose` for detailed logging
- Use `Write-Warning` for non-fatal issues
- Use `Write-Error` for errors
- Return objects, not formatted text

```powershell
# Good - returns objects
function Get-JumpSummary {
    [PSCustomObject]@{
        JumpId = $id
        Duration = $duration
        MaxAltitude = $altitude
    }
}

# Bad - returns formatted string
function Get-JumpSummary {
    "Jump $id lasted $duration seconds"
}
```

### Pipeline Support

Support PowerShell pipelines where appropriate:

```powershell
function Process-Jump {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, ValueFromPipeline)]
        [PSCustomObject]$Jump
    )
    
    begin {
        # Setup (runs once)
    }
    
    process {
        # Process each piped object
        # This block runs for each input object
    }
    
    end {
        # Cleanup (runs once)
    }
}
```

### Advanced Functions

Use `[CmdletBinding()]` for advanced function features:

```powershell
function Get-JumpMetrics {
    [CmdletBinding()]  # Enables -Verbose, -Debug, etc.
    param(
        [Parameter(Mandatory)]
        [guid]$JumpId
    )
    
    Write-Verbose "Processing jump: $JumpId"
    # Function logic
}
```

### ShouldProcess Support

For functions that make changes, implement ShouldProcess:

```powershell
function Remove-JumpData {
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory)]
        [guid]$JumpId
    )
    
    if ($PSCmdlet.ShouldProcess("Jump $JumpId", "Delete")) {
        # Perform deletion
    }
}
```

This enables `-WhatIf` and `-Confirm` parameters automatically.

## Testing

### Writing Tests

Use Pester for unit and integration tests:

```powershell
BeforeAll {
    Import-Module ./src/JumpMetrics.PowerShell/JumpMetrics.psm1 -Force
}

Describe 'Import-FlySightData' {
    It 'Should import valid FlySight file' {
        $result = Import-FlySightData -Path ./samples/sample-jump.csv
        $result.DataPoints.Count | Should -BeGreaterThan 0
    }
    
    It 'Should throw on missing file' {
        { Import-FlySightData -Path ./nonexistent.csv } | Should -Throw
    }
}
```

### Running Tests

```powershell
# Run all tests
Invoke-Pester -Path ./src/JumpMetrics.PowerShell/Tests -Output Detailed

# Run specific test file
Invoke-Pester -Path ./src/JumpMetrics.PowerShell/Tests/JumpMetrics.Tests.ps1

# Run with coverage
Invoke-Pester -Path ./src/JumpMetrics.PowerShell/Tests -CodeCoverage ./src/**/*.ps1
```

### Test Coverage

Aim for:
- 80%+ code coverage for public functions
- Test both success and failure paths
- Test edge cases (null, empty, invalid input)
- Test pipeline behavior

## Pull Request Process

1. **Create an issue** describing the feature or bug
2. **Fork and branch** from `main`
3. **Write tests** for your changes
4. **Update documentation** (README, help, examples)
5. **Run all tests** to ensure nothing breaks
6. **Lint your code** (PSScriptAnalyzer if available)
7. **Submit PR** with:
   - Clear description of changes
   - Reference to related issue
   - Screenshots if UI changes
   - Test results

### PR Checklist

- [ ] Tests pass (`Invoke-Pester`)
- [ ] Code follows PowerShell best practices
- [ ] Comment-based help is complete
- [ ] Examples are provided
- [ ] Documentation updated
- [ ] No hardcoded secrets or credentials
- [ ] Error handling is appropriate

## Code Style

### Formatting

- **Indentation:** 4 spaces (no tabs)
- **Line length:** Aim for <120 characters
- **Braces:** Opening brace on same line
- **Casing:**
  - Functions: `PascalCase` (Verb-Noun)
  - Variables: `$camelCase` or `$PascalCase`
  - Parameters: `$PascalCase`

```powershell
function Get-JumpMetrics {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [guid]$JumpId
    )
    
    $jumpData = Get-Data -Id $JumpId
    
    if ($jumpData) {
        return $jumpData
    }
}
```

### Variable Naming

```powershell
# Good
$jumpId
$dataPoints
$maxAltitude

# Avoid
$j
$dp
$ma
$temp1
```

### String Formatting

Prefer string interpolation over concatenation:

```powershell
# Good
"Processing jump $jumpId at $timestamp"

# Avoid
"Processing jump " + $jumpId + " at " + $timestamp
```

For complex formatting, use `-f`:

```powershell
"Altitude: {0:N2}m, Speed: {1:N1}m/s" -f $altitude, $speed
```

### Comments

- Write self-documenting code (clear variable/function names)
- Add comments for complex logic or non-obvious decisions
- Use comment-based help for all public functions
- Keep comments up-to-date

```powershell
# Good comment - explains why
# Use smoothing window to reduce GPS noise artifacts
$smoothedData = Get-SmoothedData -Window 5

# Bad comment - states the obvious
# Add 1 to count
$count = $count + 1
```

## .NET Development

For changes to the .NET Core library (`src/JumpMetrics.Core/`):

1. Follow C# coding conventions
2. Use dependency injection
3. Write xUnit tests
4. Document public APIs with XML comments
5. Follow SOLID principles

See the main README for .NET build instructions.

## Security

- Never commit secrets (API keys, connection strings)
- Use environment variables for configuration
- Validate all user input
- Sanitize file paths
- Follow least-privilege principles

## Documentation

- Update README.md for major features
- Create example scripts in `examples/`
- Update CLAUDE.md for architectural changes
- Add inline help for all cmdlets
- Include usage examples

## Questions?

Open an issue for:
- Feature requests
- Bug reports
- Questions about contributing
- Design discussions

## License

By contributing, you agree that your contributions will be licensed under the project's MIT License.

---

Thank you for contributing to JumpMetrics AI! 🪂
