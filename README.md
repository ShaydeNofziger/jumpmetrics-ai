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
   MetricsCalculator  ✅   Compute performance metrics:
                           • Freefall: vertical speed (avg/max), horizontal speed, track angle, time
                           • Canopy: deployment altitude, descent rate, glide ratio, max speed, pattern altitude
                           • Landing: final approach speed, touchdown vertical speed, accuracy
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

# Configure Azure Function App (for local development)
# Copy appsettings.json to local.settings.json in Functions project
cd src/JumpMetrics.Functions
cp appsettings.json local.settings.json

# Update local.settings.json with your Azure Storage connection string:
# {
#   "IsEncrypted": false,
#   "Values": {
#     "AzureWebJobsStorage": "UseDevelopmentStorage=true",
#     "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
#     "AzureStorage__ConnectionString": "your-connection-string-here"
#   }
# }

# Start the Function App locally (optional — for testing)
func start

# Import module and run (PowerShell CLI)
Import-Module ./src/JumpMetrics.PowerShell/JumpMetrics.psm1
Import-FlySightData -Path ./samples/sample-jump.csv
Get-JumpAnalysis -JumpId <returned-id>
```

**Note:** The module works fully offline with local processing and mock AI analysis when Azure is not configured.

## Azure Function API

The AnalyzeJump function provides an HTTP POST endpoint for processing FlySight CSV files:

**Endpoint:** `POST /api/jumps/analyze`

**Request:** 
- Content-Type: `text/csv` or `multipart/form-data`
- Headers: 
  - `X-FileName`: Name of the CSV file (optional, for raw body uploads)
- Body: FlySight CSV file content

**Response:** JSON object containing:
```json
{
  "jumpId": "guid",
  "jumpDate": "datetime",
  "fileName": "string",
  "blobUri": "string",
  "metadata": {
    "totalDataPoints": 1972,
    "recordingStart": "datetime",
    "recordingEnd": "datetime",
    "maxAltitude": 1910.0,
    "minAltitude": 193.0
  },
  "segments": [
    {
      "type": "Freefall",
      "startTime": "datetime",
      "endTime": "datetime",
      "startAltitude": 1910.0,
      "endAltitude": 1780.0,
      "duration": 15.0,
      "dataPointCount": 75
    }
  ],
  "metrics": {
    "freefall": {},
    "canopy": {},
    "landing": {}
  },
  "validationWarnings": []
}
```

**Example Usage (curl):**
```bash
curl -X POST http://localhost:7071/api/jumps/analyze \
  -H "X-FileName: sample-jump.csv" \
  -H "Content-Type: text/csv" \
  --data-binary @samples/sample-jump.csv
```

## CLI Commands

| Command | Description | Example |
|---|---|---|
| `Import-FlySightData` | Parse FlySight 2 CSV and return structured data | `$jump = Import-FlySightData -Path ./data.csv` |
| `Get-JumpMetrics` | Calculate performance metrics for jump phases | `Get-JumpMetrics -Jump $jump` |
| `Get-JumpAnalysis` | Generate AI-powered performance analysis | `Get-JumpAnalysis -Jump $jump` |
| `Get-JumpHistory` | List all processed jumps from session cache | `Get-JumpHistory -Count 50` |
| `Export-JumpReport` | Generate comprehensive markdown report | `Export-JumpReport -Jump $jump -OutputPath ./report.md` |

## AI-Powered Analysis

JumpMetrics AI uses Azure OpenAI (GPT-4) to provide expert-level jump analysis with:

- **Safety Flag Detection** - Automatic identification of concerning patterns:
  - Low pulls (deployment below safe altitude)
  - Aggressive canopy flight (high horizontal speeds)
  - Hard landings (high vertical touchdown speed)
  - Poor landing patterns
  - Unstable freefall

- **Structured Insights** - Each analysis includes:
  - Overall performance assessment
  - Specific strengths identified in the jump
  - Areas for improvement with actionable feedback
  - Progression recommendations for skill development
  - Skill level rating (1-10)

- **Safety-Focused Guidance** - Conservative recommendations based on:
  - USPA license level standards
  - Equipment suitability
  - Experience level considerations
  - Best practices for progression

See [docs/AI_INTEGRATION.md](docs/AI_INTEGRATION.md) for detailed documentation on configuration, safety flags, and usage examples.

## AI-Powered Analysis

JumpMetrics AI uses Azure OpenAI (GPT-4) to provide expert-level jump analysis with:

- **Safety Flag Detection** - Automatic identification of concerning patterns:
  - Low pulls (deployment below safe altitude)
  - Aggressive canopy flight (high horizontal speeds)
  - Hard landings (high vertical touchdown speed)
  - Poor landing patterns
  - Unstable freefall

- **Structured Insights** - Each analysis includes:
  - Overall performance assessment
  - Specific strengths identified in the jump
  - Areas for improvement with actionable feedback
  - Progression recommendations for skill development
  - Skill level rating (1-10)

- **Safety-Focused Guidance** - Conservative recommendations based on:
  - USPA license level standards
  - Equipment suitability
  - Experience level considerations
  - Best practices for progression

See [docs/AI_INTEGRATION.md](docs/AI_INTEGRATION.md) for detailed documentation on configuration, safety flags, and usage examples.

## Project Status

**Phase 1 (Data Ingestion)** — ✅ **Complete**
- FlySight 2 CSV parser implemented with full v2 header protocol support
- Data validator with comprehensive error and warning detection
- 24 unit tests passing (10 parser tests, 13 validator tests)
- Successfully parses real FlySight 2 sample data (1,972 data points)

**Phase 2 (Jump Segmentation)** — ✅ **Complete**
- Automatic phase detection implemented with rate-of-change algorithms
- Configurable thresholds and comprehensive test coverage (15 tests)
- Handles hop-n-pops, high pulls, and standard jumps

**Phase 3 (Metrics Calculation)** — ✅ **Complete**
- MetricsCalculator fully implemented with comprehensive test coverage (14/14 tests passing)
- Calculates freefall, canopy, and landing performance metrics from segmented jump data
- See [MetricsCalculator.cs](src/JumpMetrics.Core/Services/Metrics/MetricsCalculator.cs) for implementation details

**Phase 4 (Azure Integration)** — ✅ **Complete**
- Azure Function App with HTTP POST trigger implemented
- Blob and Table storage integration complete
- DI configured for all Core services
- Integration tests passing

**Phase 5 (AI Integration)** — ✅ **Complete**
- Azure OpenAI service integrated with GPT-4 analysis agent
- Safety flag detection (low pulls, aggressive canopy, hard landings)
- Structured JSON output parsing to AIAnalysis model
- System prompt engineered with skydiving domain expertise
- 9 unit tests passing, 2 integration test examples included

### Phase 4 Implementation Summary

The Azure integration enables cloud-based processing of FlySight CSV files with persistent storage:

**Key Components:**

1. **Storage Service** (`IStorageService`)
   - Blob storage for FlySight CSV files in `flysight-files` container
   - Table storage for jump metrics in `JumpMetrics` table (partitioned by month)
   - Support for uploading, storing, and retrieving jump data

2. **AnalyzeJump Function** (`POST /api/jumps/analyze`)
   - Accepts CSV uploads via HTTP POST (raw body or multipart)
   - Orchestrates full processing pipeline: Parse → Validate → Segment → Calculate → Store
   - Returns JSON with jumpId, metadata, segments, metrics, and validation warnings
   - Error handling with appropriate HTTP status codes (400 for validation, 500 for errors)

3. **Dependency Injection**
   - All Core services registered in `Program.cs` (Parser, Validator, Segmenter, Calculator, Storage, AI Analysis)
   - Azure Storage clients configured with connection strings
   - Supports both development storage (Azurite) and production Azure Storage

4. **Configuration**
   - Connection string: `AzureStorage:ConnectionString` or `AzureWebJobsStorage`
   - Azure OpenAI: `AzureOpenAI:Endpoint`, `AzureOpenAI:ApiKey`, `AzureOpenAI:DeploymentName`
   - Local development: Use `local.settings.json` (template provided)
   - Default: `UseDevelopmentStorage=true` for local testing with Azurite

5. **Testing**
   - 8 integration tests verify service contracts and DI construction
   - All tests passing (100% success rate)
   - Moq framework for mocking dependencies
- Safety flag detection (low pulls, aggressive canopy, hard landings)
- Structured JSON output parsing to AIAnalysis model
- System prompt engineered with skydiving domain expertise
- 9 unit tests passing, 2 integration test examples included

**Phase 6 (CLI & Documentation)** — ✅ **Complete**
- All PowerShell cmdlets implemented and tested
- Comprehensive help documentation with examples
- 4 example workflow scripts in `examples/` directory
- CONTRIBUTING.md with PowerShell best practices
- 9 Pester tests passing
- Full offline functionality with mock AI analysis

### Phases 1-5 Implementation Summary

The core .NET library and Azure Functions are fully implemented with all components tested and integrated:

**Phase 1-3: Core Processing Pipeline**
- FlySight 2 CSV parser with full v2 header protocol support (24 tests passing)
- Jump segmentation with automatic phase detection and configurable thresholds (15 tests passing)
- Performance metrics calculation for freefall, canopy, and landing (14 tests passing)

**Phase 4: Azure Integration**

The Azure integration enables cloud-based processing of FlySight CSV files with persistent storage:

**Key Components:**

1. **Storage Service** (`IStorageService`)
   - Blob storage for FlySight CSV files in `flysight-files` container
   - Table storage for jump metrics in `JumpMetrics` table (partitioned by month)
   - Support for uploading, storing, and retrieving jump data

2. **AnalyzeJump Function** (`POST /api/jumps/analyze`)
   - Accepts CSV uploads via HTTP POST (raw body or multipart)
   - Orchestrates full processing pipeline: Parse → Validate → Segment → Calculate → Store
   - Returns JSON with jumpId, metadata, segments, metrics, and validation warnings
   - Error handling with appropriate HTTP status codes (400 for validation, 500 for errors)

3. **Dependency Injection**
   - All Core services registered in `Program.cs` (Parser, Validator, Segmenter, Calculator, Storage, AI Analysis)
   - Azure Storage clients configured with connection strings
   - Supports both development storage (Azurite) and production Azure Storage

4. **Configuration**
   - Connection string: `AzureStorage:ConnectionString` or `AzureWebJobsStorage`
   - Azure OpenAI: `AzureOpenAI:Endpoint`, `AzureOpenAI:ApiKey`, `AzureOpenAI:DeploymentName`
   - Local development: Use `local.settings.json` (template provided)
   - Default: `UseDevelopmentStorage=true` for local testing with Azurite

5. **Testing**
   - 8 integration tests verify service contracts and DI construction
   - All tests passing (100% success rate)
   - Moq framework for mocking dependencies

**Phase 5: AI-Powered Analysis**
- Azure OpenAI integration with GPT-4 for intelligent jump analysis
- Safety-focused recommendations and skill level assessment
- See [docs/AI_INTEGRATION.md](docs/AI_INTEGRATION.md) for detailed documentation

See [CLAUDE.md](CLAUDE.md) for the full project specification and detailed implementation for all phases.

## Contributing

We welcome contributions! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for:
- Development setup instructions
- PowerShell best practices
- Testing guidelines
- Pull request process
- Code style guide

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
