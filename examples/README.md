# JumpMetrics AI - Example Scripts

Example workflows demonstrating the JumpMetrics PowerShell module.

## Prerequisites

- PowerShell 7.5+
- JumpMetrics module (`../src/JumpMetrics.PowerShell/`)

## Examples

### 01-basic-workflow.ps1
Interactive workflow: import, analyze, and view jump data.
```powershell
./examples/01-basic-workflow.ps1
```

### 02-generate-report.ps1
Generate comprehensive markdown reports.
```powershell
./examples/02-generate-report.ps1 -InputPath ./samples/sample-jump.csv -OutputPath ./report.md
```

### 03-batch-processing.ps1
Process multiple FlySight files at once.
```powershell
./examples/03-batch-processing.ps1 -InputDirectory ./my-jumps -OutputDirectory ./reports
```

### 04-pipeline-example.ps1
PowerShell pipeline usage examples.
```powershell
./examples/04-pipeline-example.ps1
```

## Quick Start

```powershell
# From repository root
./examples/01-basic-workflow.ps1
```

## Manual Usage

```powershell
Import-Module ./src/JumpMetrics.PowerShell/JumpMetrics.psm1

$jump = Import-FlySightData -Path ./samples/sample-jump.csv
$metrics = Get-JumpMetrics -Jump $jump
$analysis = Get-JumpAnalysis -Jump $jump -Metrics $metrics
Export-JumpReport -Jump $jump -Metrics $metrics -Analysis $analysis -OutputPath ./report.md
```

## Available Cmdlets

- `Import-FlySightData` - Parse FlySight 2 CSV
- `Get-JumpMetrics` - Calculate performance metrics
- `Get-JumpAnalysis` - Generate AI analysis
- `Get-JumpHistory` - List processed jumps
- `Export-JumpReport` - Generate markdown report

## Help

```powershell
Get-Help Import-FlySightData -Full
Get-Help Get-JumpMetrics -Examples
```

## Azure (Optional)

```powershell
$env:AZURE_STORAGE_CONNECTION_STRING = "..."
$env:AZURE_OPENAI_ENDPOINT = "..."
$env:AZURE_OPENAI_KEY = "..."
```
