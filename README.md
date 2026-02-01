# JumpMetrics AI

Intelligent skydiving performance analysis tool that processes FlySight 2 GPS data to provide AI-powered insights, performance metrics, and safety recommendations.

## Features

- **FlySight 2 CSV Parsing** - Ingest and validate GPS data from FlySight 2 devices, including the v2 header protocol (`$FLYS`, `$VAR`, `$COL`, `$DATA`)
- **Automatic Jump Segmentation** ✅ - Detect jump phases: aircraft, exit, freefall, deployment, canopy flight, and landing using rate-of-change detection and configurable thresholds
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

```powershell
# Clone
git clone https://github.com/ShaydeNofziger/jumpmetrics-ai.git
cd jumpmetrics-ai

# Build and test
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release

# Run PowerShell Pester tests
Install-Module -Name Pester -Force -SkipPublisherCheck
Invoke-Pester -Path ./src/JumpMetrics.PowerShell/Tests -Output Detailed

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

## CLI Commands

| Command | Description |
|---|---|
| `Import-FlySightData` | Parse and upload a FlySight CSV file |
| `Get-JumpAnalysis` | Retrieve AI analysis for a jump |
| `Get-JumpMetrics` | Display calculated performance metrics |
| `Get-JumpHistory` | View all processed jumps |
| `Export-JumpReport` | Generate a markdown report |

## Project Status

**Phase 1 (Data Ingestion)** — Scaffolding complete. Real FlySight v2 sample data integrated. Parser and validator implementations are next.

**Phase 2 (Jump Segmentation)** ✅ — Complete. Automatic phase detection implemented with rate-of-change algorithms, configurable thresholds, and comprehensive test coverage (15 tests).

See [CLAUDE.md](CLAUDE.md) for the full project specification, detailed requirements, and implementation phases.

## Jump Segmentation

The JumpSegmenter analyzes GPS velocity and altitude data to automatically detect and classify jump phases:

### Supported Phases
- **Aircraft** - Pre-jump climb phase (negative velD or increasing altitude)
- **Exit** - Transition from aircraft to freefall at peak altitude
- **Freefall** - Accelerating descent phase (10+ m/s, handles hop-n-pops and terminal velocity)
- **Deployment** - Parachute opening with sharp deceleration (5 m/s² threshold)
- **Canopy** - Stable descent under canopy (2-15 m/s range)
- **Landing** - Final approach and touchdown (altitude plateau, velocities → 0)

### Algorithm Features
- **Rate-of-change detection** - Uses velocity derivatives, not just absolute thresholds
- **GPS noise filtering** - Sliding window smoothing (configurable window size)
- **Accuracy filtering** - Filters points with horizontal accuracy > 50m (configurable)
- **Configurable thresholds** - 10+ parameters via `SegmentationOptions` class
- **Edge case handling** - Hop-n-pops, turbulent canopy, ground recordings, mid-jump starts

### Usage Example
```csharp
using JumpMetrics.Core.Models;
using JumpMetrics.Core.Services.Segmentation;

// Use default options
var segmenter = new JumpSegmenter();

// Or configure custom thresholds
var options = new SegmentationOptions
{
    MinFreefallVelD = 8.0,           // Lower threshold for hop-n-pops
    DeploymentDecelThreshold = 3.0,   // More sensitive deployment detection
    GpsAccuracyThreshold = 30.0       // Stricter GPS accuracy requirement
};
var customSegmenter = new JumpSegmenter(options);

// Segment the jump
var segments = segmenter.Segment(dataPoints);

// Process results
foreach (var segment in segments)
{
    Console.WriteLine($"{segment.Type}: {segment.Duration:F1}s, " +
                     $"{segment.StartAltitude:F0}m → {segment.EndAltitude:F0}m");
}
```

### Test Coverage
- **12 unit tests** - Edge cases with synthetic data (null inputs, poor GPS, ground recordings, turbulent canopy)
- **3 integration tests** - Realistic FlySight data patterns (hop-n-pop, full altitude jump, turbulent canopy)

## License

MIT License - See [LICENSE](LICENSE) for details.
