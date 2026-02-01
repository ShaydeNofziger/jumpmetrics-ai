# JumpMetrics AI

Intelligent skydiving performance analysis tool that processes FlySight 2 GPS data to provide AI-powered insights, performance metrics, and safety recommendations.

## Features

- **FlySight 2 CSV Parsing** - Ingest and validate GPS data from FlySight 2 devices, including the v2 header protocol (`$FLYS`, `$VAR`, `$COL`, `$DATA`)
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
examples/
  01-basic-workflow.ps1        Interactive tutorial workflow
  02-generate-report.ps1       Parameterized report generator
  03-batch-processing.ps1      Multi-file batch processor
  04-pipeline-example.ps1      PowerShell pipeline patterns
  README.md                    Examples documentation
samples/
  sample-jump.csv              Real FlySight 2 recording (~1,972 data points, hop-n-pop jump)
```

## FlySight 2 Data Format

JumpMetrics AI parses FlySight 2's structured CSV format. Files are **not** plain CSVs — they include a header protocol that defines metadata, column ordering, and units before the data section.

```
$FLYS,1                                            # Format version
$VAR,FIRMWARE_VER,v2023.09.22.2                    # Device firmware
$VAR,DEVICE_ID,00190037484e501420353131            # Hardware ID
$VAR,SESSION_ID,88e923ed802cfc8f2ade9528           # Session ID
$COL,GNSS,time,lat,lon,hMSL,velN,velE,velD,hAcc,vAcc,sAcc,numSV
$UNIT,GNSS,,deg,deg,m,m/s,m/s,m/s,m,m,m/s,
$DATA
$GNSS,2025-09-11T17:26:18.600Z,34.7636471,-81.2017957,959.293,...
```

Key fields per data row:

| Field | Description |
|---|---|
| `time` | ISO 8601 timestamp (millisecond precision) |
| `lat`, `lon` | WGS84 coordinates (degrees) |
| `hMSL` | Altitude above mean sea level (meters) |
| `velN`, `velE`, `velD` | Velocity north/east/down (m/s). `velD` positive = descending |
| `hAcc`, `vAcc` | Horizontal/vertical accuracy (meters) |
| `sAcc` | Speed accuracy (m/s) |
| `numSV` | Number of satellites in fix |

## Processing Pipeline

```
FlySight 2 GPS Device
        |  CSV file
        v
   FlySightParser         Parse v2 header protocol + $GNSS data rows
        |
        v
   DataValidator           Flag GPS noise, check data integrity
        |
        v
   JumpSegmenter           Detect phases: aircraft → exit → freefall → deployment → canopy → landing
        |
        v
   MetricsCalculator       Compute freefall speed, glide ratio, descent rates, etc.
        |
        v
   Azure OpenAI (GPT-4)    AI analysis: safety flags, performance assessment, recommendations
        |
        v
   PowerShell CLI Output    Formatted metrics, AI insights, markdown reports
```

## Sample Data

The included `samples/sample-jump.csv` is a real FlySight 2 recording of a hop-n-pop skydive:

| Property | Value |
|---|---|
| Data points | 1,972 |
| Recording rate | 5 Hz |
| Duration | ~6 min 30 sec |
| Exit altitude | ~1,910m MSL (~5,630 ft AGL) |
| Peak vertical speed | ~25 m/s (~56 mph) |
| Ground elevation | ~193m MSL |

Jump phases in the data: GPS acquisition → aircraft climb (960m → 1,910m) → exit → short freefall (~15 sec) → deployment → canopy flight (~4 min) → landing at ~193m MSL.

## Prerequisites

- PowerShell 7.5+
- .NET 10 SDK
- Azure subscription (for cloud features)
- Azure CLI
- Git

## Getting Started

### Quick Start

```powershell
# Clone the repository
git clone https://github.com/ShaydeNofziger/jumpmetrics-ai.git
cd jumpmetrics-ai

# Import the PowerShell module
Import-Module ./src/JumpMetrics.PowerShell/JumpMetrics.psm1

# Import and analyze a FlySight file
$jump = Import-FlySightData -Path ./samples/sample-jump.csv
$metrics = Get-JumpMetrics -Jump $jump
$analysis = Get-JumpAnalysis -Jump $jump
Export-JumpReport -Jump $jump -Metrics $metrics -Analysis $analysis -OutputPath ./report.md
```

### Complete Workflow Example

```powershell
# 1. Import FlySight 2 GPS data
$jump = Import-FlySightData -Path ./samples/sample-jump.csv

# Output:
# Importing FlySight data from: sample-jump.csv
# ✓ Successfully parsed 1972 data points
#   Jump ID: db960ed5-662c-43b9-b144-280138e92851
#   Recording: 2025-09-11 17:26:18 to 17:32:52
#   Altitude: 191.6m to 1910.7m MSL
#   Duration: 394.2s

# 2. Calculate performance metrics
$metrics = Get-JumpMetrics -Jump $jump

# Output shows:
# - Aircraft climb phase (117.6s, 959m → 1910m MSL)
# - Freefall (17.2s, avg 8.3 m/s, max 24.8 m/s)
# - Deployment (2.8s, 8.8 m/s deceleration)
# - Canopy flight (217.6s, glide ratio 2.42:1, 3519m distance)
# - Landing approach

# 3. Get AI-powered analysis (mock if Azure not configured)
$analysis = Get-JumpAnalysis -Jump $jump -Metrics $metrics

# Output includes:
# - Overall performance assessment
# - Skill level rating (1-10)
# - Safety flags and warnings
# - Strengths identified
# - Areas for improvement
# - Progression recommendations

# 4. Generate markdown report
Export-JumpReport -Jump $jump -Metrics $metrics -Analysis $analysis -OutputPath ./my-jump-report.md

# 5. View jump history
Get-JumpHistory

# Lists all processed jumps with:
# - Jump ID, Date, File name
# - Duration, Max altitude
```

### PowerShell Pipeline Usage

Take advantage of PowerShell's pipeline for concise workflows:

```powershell
# Import and analyze in one line
Import-FlySightData -Path ./samples/sample-jump.csv | Get-JumpMetrics

# Full pipeline
$jump = Import-FlySightData -Path ./samples/sample-jump.csv
$metrics = $jump | Get-JumpMetrics
Get-JumpAnalysis -Jump $jump -Metrics $metrics

# Batch process multiple files
Get-ChildItem ./my-jumps/*.csv | ForEach-Object {
    $jump = Import-FlySightData -Path $_.FullName
    $metrics = Get-JumpMetrics -Jump $jump
    Export-JumpReport -Jump $jump -Metrics $metrics -OutputPath "./reports/$($_.BaseName)-report.md"
}
```

### Example Scripts

Pre-built workflow examples are available in the `examples/` directory:

```powershell
# Interactive basic workflow
./examples/01-basic-workflow.ps1

# Generate a report from a file
./examples/02-generate-report.ps1 -InputPath ./samples/sample-jump.csv -OutputPath ./report.md

# Batch process multiple files
./examples/03-batch-processing.ps1 -InputDirectory ./my-jumps -OutputDirectory ./reports

# PowerShell pipeline examples
./examples/04-pipeline-example.ps1
```

See [examples/README.md](examples/README.md) for detailed documentation.

## .NET Development

Build and test the .NET components:

```powershell
# Build and test
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release

# Run PowerShell Pester tests
Invoke-Pester -Path ./src/JumpMetrics.PowerShell/Tests -Output Detailed
```

## Azure Configuration (Optional)

For cloud storage and real AI analysis, configure Azure:

```powershell
# Deploy Azure resources (optional — for cloud features)
az login
az deployment group create --resource-group jumpmetrics-rg --template-file infrastructure/main.bicep

# Configure environment (optional — for cloud features)
$env:AZURE_STORAGE_CONNECTION_STRING = "your-connection-string"
$env:AZURE_OPENAI_ENDPOINT = "your-openai-endpoint"
$env:AZURE_OPENAI_KEY = "your-api-key"

# Import module and run
Import-Module ./src/JumpMetrics.PowerShell/JumpMetrics.psm1
Import-FlySightData -Path ./samples/sample-jump.csv
Get-JumpAnalysis -JumpId <returned-id>
```

**Note:** The module works fully offline with local processing and mock AI analysis when Azure is not configured.

## CLI Commands

| Command | Description | Example |
|---|---|---|
| `Import-FlySightData` | Parse FlySight 2 CSV and return structured data | `$jump = Import-FlySightData -Path ./data.csv` |
| `Get-JumpMetrics` | Calculate performance metrics for jump phases | `Get-JumpMetrics -Jump $jump` |
| `Get-JumpAnalysis` | Generate AI-powered performance analysis | `Get-JumpAnalysis -Jump $jump` |
| `Get-JumpHistory` | List all processed jumps from session cache | `Get-JumpHistory -Count 50` |
| `Export-JumpReport` | Generate comprehensive markdown report | `Export-JumpReport -Jump $jump -OutputPath ./report.md` |

### Detailed Command Help

All cmdlets include comprehensive help documentation:

```powershell
# View full help for a command
Get-Help Import-FlySightData -Full

# View examples only
Get-Help Get-JumpMetrics -Examples

# List all parameters
Get-Help Export-JumpReport -Parameter *
```

## Project Status

**Phase 1 (Data Ingestion)** — ✅ Complete  
- FlySight 2 CSV parser with full v2 header protocol support
- Data validation and quality checks
- PowerShell and .NET implementations

**Phase 6 (CLI & Documentation)** — ✅ Complete  
- All PowerShell cmdlets implemented
- Comprehensive help documentation
- Example workflow scripts
- CONTRIBUTING.md with PowerShell best practices
- Full Pester test coverage

**Next Steps:**
- Phase 2: Jump segmentation (detect phases: aircraft, freefall, deployment, canopy, landing)
- Phase 3: Advanced metrics calculation
- Phase 4: Azure Functions integration
- Phase 5: Real Azure OpenAI integration

See [CLAUDE.md](CLAUDE.md) for the full project specification and detailed implementation phases.

## Contributing

We welcome contributions! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for:
- Development setup instructions
- PowerShell best practices
- Testing guidelines
- Pull request process
- Code style guide

## License

MIT License - See [LICENSE](LICENSE) for details.
