# JumpMetrics AI

Intelligent skydiving performance analysis tool that processes FlySight 2 GPS data to provide AI-powered insights, performance metrics, and safety recommendations.

## Features

- **FlySight 2 CSV Parsing** - Ingest and validate GPS data from FlySight 2 devices
- **Automatic Jump Segmentation** - Detect jump phases: exit, freefall, deployment, canopy flight, and landing
- **Performance Metrics** - Calculate freefall speed, glide ratio, canopy descent rate, landing accuracy, and more
- **AI-Powered Analysis** - GPT-4 driven performance assessment, safety flags, and progression recommendations
- **PowerShell CLI** - Full-featured command-line interface for processing and reporting

## Tech Stack

| Component | Technology |
|---|---|
| CLI | PowerShell 7.5+ |
| Processing | .NET 10 |
| AI | Azure OpenAI (GPT-4) |
| Compute | Azure Functions v4 (isolated worker) |
| Storage | Azure Blob + Table Storage |
| CI/CD | GitHub Actions |
| IaC | Bicep |

## Repository Structure

```
src/
  JumpMetrics.Core/            .NET 10 class library (models, interfaces, services)
  JumpMetrics.Functions/       Azure Functions v4 isolated worker (HTTP triggers)
  JumpMetrics.PowerShell/      PowerShell 7.5 module (CLI cmdlets + Pester tests)
tests/
  JumpMetrics.Core.Tests/      xUnit tests for Core library
  JumpMetrics.Functions.Tests/  xUnit tests for Functions
infrastructure/
  main.bicep                   Azure resources (Storage, Functions, OpenAI, App Insights)
  main.bicepparam              Parameter file
samples/
  sample-jump.csv              FlySight 2 sample data
```

## Prerequisites

- PowerShell 7.5+
- .NET 10 SDK
- Azure subscription
- Azure CLI
- Git

## Getting Started

```powershell
# Clone
git clone https://github.com/yourusername/JumpMetricsAI.git
cd JumpMetricsAI

# Install dependencies
dotnet restore

# Build and test
dotnet build --configuration Release
dotnet test --configuration Release

# Deploy Azure resources
az login
az deployment group create --resource-group jumpmetrics-rg --template-file infrastructure/main.bicep

# Configure
$env:AZURE_STORAGE_CONNECTION_STRING = "your-connection-string"
$env:AZURE_OPENAI_ENDPOINT = "your-openai-endpoint"
$env:AZURE_OPENAI_KEY = "your-api-key"

# Import module and run
Import-Module ./src/JumpMetrics.PowerShell/JumpMetrics.psm1
Import-FlySightData -Path ./samples/sample-jump.csv
Get-JumpAnalysis -JumpId <returned-id>
```

## CLI Commands

| Command | Description |
|---|---|
| `Import-FlySightData` | Parse and upload a FlySight CSV file |
| `Get-JumpAnalysis` | Retrieve AI analysis for a jump |
| `Get-JumpMetrics` | Display calculated performance metrics |
| `Get-JumpHistory` | View all processed jumps |
| `Export-JumpReport` | Generate a markdown report |

## Architecture

```
FlySight 2 GPS Device
        |  CSV Export
        v
PowerShell CLI Module (CSV Parser + Azure Upload)
        |
        v
Azure Storage (Blob: CSV files, Table: Metrics cache)
        |
        v
.NET 10 Processing Service (Segmentation, Metrics, Validation)
        |
        v
Azure OpenAI (GPT-4 Analysis Agent)
        |
        v
CLI Output (Formatted metrics, AI insights, Reports)
```

## Project Status

Skeleton complete. Implementation in progress.

## License

MIT License - See [LICENSE](LICENSE) for details.
