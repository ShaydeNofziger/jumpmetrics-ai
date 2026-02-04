# JumpMetrics AI

Intelligent skydiving performance analysis tool that processes FlySight 2 GPS data locally to provide performance metrics, jump segmentation, and safety insights.

## Features

- **Local Processing** - All data processing happens on your machine, no cloud services required
- **FlySight 2 CSV Parsing** - Ingest and validate GPS data from FlySight 2 devices, including the v2 header protocol (`$FLYS`, `$VAR`, `$COL`, `$DATA`)
- **Automatic Jump Segmentation** - Detect jump phases: aircraft, exit, freefall, deployment, canopy flight, and landing using rate-of-change detection
- **Performance Metrics** - Calculate freefall speed, glide ratio, canopy descent rate, landing accuracy, and more
- **Optional AI Analysis** - Integrate with Azure OpenAI for AI-powered performance assessment and recommendations
- **PowerShell CLI** - Full-featured command-line interface for processing and reporting

## Quick Start

### Prerequisites
- PowerShell 7.5+
- .NET 10 SDK

### Build

```bash
# Clone the repository
git clone https://github.com/ShaydeNofziger/jumpmetrics-ai.git
cd jumpmetrics-ai

# Build the CLI tool
dotnet build src/JumpMetrics.CLI/JumpMetrics.CLI.csproj --configuration Release

# Run example
pwsh examples/01-local-analysis.ps1
```

### Usage

```powershell
# Import the module
Import-Module ./src/JumpMetrics.PowerShell/JumpMetrics.psm1

# Process a FlySight CSV file
$jump = Import-FlySightData -Path ./samples/sample-jump.csv

# Display metrics
Get-JumpMetrics -JumpData $jump

# Generate a report
Export-JumpReport -JumpData $jump -OutputPath ./reports/my-jump.md

# Save to local storage for later
Import-FlySightData -Path ./samples/sample-jump.csv -SaveToStorage

# View jump history
Get-JumpHistory
```

## Repository Structure

```
src/
  JumpMetrics.Core/            .NET 10 class library (models, interfaces, services)
  JumpMetrics.CLI/             Command-line tool for local processing
  JumpMetrics.PowerShell/      PowerShell 7.5 module (CLI cmdlets + Pester tests)
tests/
  JumpMetrics.Core.Tests/      xUnit tests for Core library (60+ test cases)
samples/
  sample-jump.csv              Real FlySight 2 recording (~1,972 data points, hop-n-pop jump)
examples/
  01-local-analysis.ps1        Local FlySight file analysis
reports/
  local-analysis-report.md     Sample output report
docs/
  AI_INTEGRATION.md            Azure OpenAI configuration guide (optional)
  AI_USAGE_EXAMPLES.md         AI analysis examples (optional)
```

## Processing Pipeline

```
FlySight 2 GPS Device
        |  CSV file
        v
   FlySightParser         Parse v2 header protocol + $GNSS data rows
        |
        v
   DataValidator          Flag GPS noise, check data integrity
        |
        v
   JumpSegmenter          Detect phases: aircraft → exit → freefall → deployment → canopy → landing
        |
        v
   MetricsCalculator      Compute performance metrics:
                          • Freefall: vertical speed (avg/max), horizontal speed, track angle, time
                          • Canopy: deployment altitude, descent rate, glide ratio, max speed, pattern altitude
                          • Landing: final approach speed, touchdown vertical speed, accuracy
        |
        v
   Export-JumpReport      Generate markdown report
```

## Tech Stack

| Component | Technology |
|---|---|
| CLI | PowerShell 7.5+ |
| Processing | .NET 10 |
| Local Storage | JSON files |
| AI (Optional) | Azure OpenAI (GPT-4) |
| CI/CD | GitHub Actions |

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

## Local Storage

Processed jumps are stored as JSON files in `~/.jumpmetrics/jumps/` by default:

```powershell
# Save jump data
Import-FlySightData -Path ./jump.csv -SaveToStorage

# Custom storage location
Import-FlySightData -Path ./jump.csv -SaveToStorage -StoragePath "C:\MyJumps"

# List saved jumps
Get-JumpHistory

# Load jump by ID
Get-JumpMetrics -JumpId "guid-here"
```

## AI Integration (Optional)

AI analysis requires Azure OpenAI credentials. See [docs/AI_INTEGRATION.md](docs/AI_INTEGRATION.md) for setup instructions.

```powershell
$jump = Import-FlySightData -Path ./jump.csv
Get-JumpAnalysis -JumpData $jump -GenerateWithAI \
    -OpenAIEndpoint "https://your-endpoint.openai.azure.com" \
    -OpenAIKey "your-api-key"
```

**Note**: AI analysis is entirely optional. All core functionality (parsing, segmentation, metrics calculation, reporting) works completely offline.

## Sample Output

See [reports/local-analysis-report.md](reports/local-analysis-report.md) for a complete example report including:
- Jump metadata (device, firmware, timestamps)
- Segment breakdown (freefall, canopy, landing durations and altitudes)
- Performance metrics (speeds, glide ratio, etc.)

## Development

```bash
# Build
dotnet build

# Run tests
dotnet test

# Run Pester tests (PowerShell module)
pwsh -c "Install-Module -Name Pester -Force; Invoke-Pester ./src/JumpMetrics.PowerShell/Tests"
```

## License

MIT License - see [LICENSE](LICENSE) for details.

## Contributing

Contributions welcome! Please open an issue or submit a pull request.
