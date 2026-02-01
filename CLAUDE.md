# JumpMetrics AI - Project Specification

## Project Overview

JumpMetrics AI is an intelligent skydiving performance analysis tool that processes FlySight 2 GPS data to provide AI-powered insights, performance metrics, and safety recommendations for skydivers. The project demonstrates expertise in PowerShell automation, .NET development, Azure cloud services, and AI agent orchestration.

**Primary Goals:**
- Parse and analyze FlySight 2 GPS data files
- Automatically segment jumps into phases (exit, freefall, deployment, canopy flight, landing)
- Calculate meaningful performance metrics for each phase
- Provide AI-generated insights, safety recommendations, and progression tracking
- Demonstrate modern cloud-native architecture with AI integration

**Technology Stack:**
- **PowerShell 7.5+**: CLI interface and FlySight data parsing module
- **.NET 10**: Core class library and Azure Functions processing service
- **Azure OpenAI**: GPT-4 agent for intelligent analysis
- **Azure Functions v4**: Isolated worker process for analysis orchestration
- **Azure Storage**: Blob storage (FlySight files) and Table storage (metrics)
- **GitHub Actions**: CI/CD pipeline and automated testing
- **Bicep**: Infrastructure as code

---

## Repository Structure

```
JumpMetricsAI/
├── .github/
│   └── workflows/
│       └── ci.yml                          # Build, test (.NET + Pester) on push/PR
├── infrastructure/
│   ├── main.bicep                          # Azure resources (Storage, Functions, OpenAI, App Insights)
│   └── main.bicepparam                     # Parameter file
├── samples/
│   └── sample-jump.csv                     # FlySight 2 sample data
├── src/
│   ├── JumpMetrics.Core/                   # .NET 10 class library
│   │   ├── Interfaces/
│   │   │   ├── IAIAnalysisService.cs       # AI analysis contract
│   │   │   ├── IDataValidator.cs           # Data validation contract + ValidationResult
│   │   │   ├── IFlySightParser.cs          # CSV parser contract
│   │   │   ├── IJumpSegmenter.cs           # Segmentation contract
│   │   │   └── IMetricsCalculator.cs       # Metrics calculation contract
│   │   ├── Models/
│   │   │   ├── AIAnalysis.cs               # AI output + SafetyFlag + SafetySeverity
│   │   │   ├── DataPoint.cs                # Single GPS record with derived properties
│   │   │   ├── Jump.cs                     # Top-level jump aggregate
│   │   │   ├── JumpMetadata.cs             # Recording metadata
│   │   │   ├── JumpPerformanceMetrics.cs   # Freefall, Canopy, Landing metrics
│   │   │   ├── JumpSegment.cs              # Time/altitude bounded segment
│   │   │   └── SegmentType.cs              # Exit, Freefall, Deployment, Canopy, Landing
│   │   └── Services/
│   │       ├── FlySightParser.cs           # IFlySightParser implementation
│   │       ├── Metrics/
│   │       │   └── MetricsCalculator.cs    # IMetricsCalculator implementation
│   │       ├── Segmentation/
│   │       │   └── JumpSegmenter.cs        # IJumpSegmenter implementation
│   │       └── Validation/
│   │           └── DataValidator.cs        # IDataValidator implementation
│   ├── JumpMetrics.Functions/              # Azure Functions v4 isolated worker
│   │   ├── AnalyzeJumpFunction.cs          # HTTP trigger: POST /api/jumps/analyze
│   │   ├── Program.cs                      # Host builder with DI registration
│   │   ├── host.json                       # Functions host config
│   │   └── local.settings.json             # Local dev settings (gitignored)
│   └── JumpMetrics.PowerShell/             # PowerShell 7.5 module
│       ├── JumpMetrics.psd1                # Module manifest
│       ├── JumpMetrics.psm1                # Module loader (dot-sources Public/ and Private/)
│       ├── Public/
│       │   ├── Import-FlySightData.ps1     # Parse and upload FlySight CSV
│       │   ├── Get-JumpAnalysis.ps1        # Retrieve AI analysis
│       │   ├── Get-JumpMetrics.ps1         # Display performance metrics
│       │   ├── Get-JumpHistory.ps1         # List processed jumps
│       │   └── Export-JumpReport.ps1       # Generate markdown report
│       ├── Private/
│       │   └── ConvertFrom-FlySightCsv.ps1 # Internal CSV parsing helper
│       └── Tests/
│           └── JumpMetrics.Tests.ps1       # Pester tests
├── tests/
│   ├── JumpMetrics.Core.Tests/             # xUnit tests for Core library
│   │   ├── FlySightParserTests.cs
│   │   ├── JumpSegmenterTests.cs
│   │   ├── MetricsCalculatorTests.cs
│   │   └── DataValidatorTests.cs
│   └── JumpMetrics.Functions.Tests/        # xUnit tests for Functions
│       └── AnalyzeJumpFunctionTests.cs
├── .editorconfig                           # Code style (4-space indent, LF, UTF-8)
├── .gitignore
├── CLAUDE.md                               # This file - project specification
├── Directory.Build.props                   # Shared build settings (warnings as errors)
├── JumpMetricsAI.slnx                      # .NET solution
└── README.md
```

---

## Project Scope

### In Scope (MVP)

**Phase 1: Data Ingestion & Parsing**
- FlySight 2 CSV file parser
- Data validation and integrity checks
- Automatic detection of jump start/end based on altitude and ground speed
- Support for single jump file processing

**Phase 2: Jump Segmentation**
- Automatic phase detection:
  - Exit (aircraft departure)
  - Freefall (exit to deployment)
  - Deployment (parachute opening sequence)
  - Canopy flight (full flight to final approach)
  - Landing approach (final 500ft to touchdown)
- Time-based and altitude-based segment boundaries

**Phase 3: Performance Metrics**
- **Freefall Metrics:**
  - Average vertical speed (fall rate)
  - Maximum vertical speed
  - Average horizontal speed
  - Track angle and efficiency
  - Time in freefall

- **Canopy Flight Metrics:**
  - Deployment altitude
  - Average descent rate under canopy
  - Glide ratio (horizontal distance / altitude lost)
  - Maximum horizontal speed (swoop speed if applicable)
  - Total canopy time
  - Pattern altitude (when entering landing pattern)

- **Landing Metrics:**
  - Final approach speed
  - Touchdown vertical speed
  - Landing accuracy (if target coordinates provided)

**Phase 4: AI Analysis Agent**
- Azure OpenAI integration with GPT-4
- Specialized system prompts for skydiving knowledge
- Analysis capabilities:
  - Performance assessment for each jump phase
  - Safety flag identification (e.g., low pulls, aggressive canopy flight)
  - Comparison to typical performance for license/experience level
  - Progression recommendations
  - Equipment suitability assessment

**Phase 5: CLI Interface**
- PowerShell module: `JumpMetrics`
- Commands:
  - `Import-FlySightData` - Parse and upload FlySight file
  - `Get-JumpAnalysis` - Retrieve AI analysis for a jump
  - `Get-JumpMetrics` - Display calculated metrics
  - `Get-JumpHistory` - View all processed jumps
  - `Export-JumpReport` - Generate markdown report

**Phase 6: Cloud Infrastructure**
- Azure Function App for processing orchestration
- Azure Storage Account (Blob + Table)
- Azure OpenAI service deployment
- Bicep templates for infrastructure as code

### Out of Scope (Future Enhancements)

**Backlog - Future Features:**
- Multi-jump batch processing
- Web dashboard UI for visualization
- Comparative analysis (compare jumps to each other)
- Competition scoring (swooping courses, accuracy landings)
- Real-time GPS tracking integration
- Mobile app integration
- Social features (share jumps, compare with friends)
- Weather data correlation
- Video overlay generation (metrics on jump video)
- Equipment database integration
- Automatic logbook generation
- Formation skydiving pattern analysis
- Wingsuit-specific metrics
- Team jump coordination analysis

---

## Architecture

### High-Level Design

```
┌─────────────────┐
│   FlySight 2    │
│   GPS Device    │
└────────┬────────┘
         │ CSV Export
         ▼
┌─────────────────────────────────────────────────┐
│           PowerShell CLI Module                  │
│  ┌──────────────┐  ┌─────────────────────────┐ │
│  │ CSV Parser   │  │  Azure Storage Upload   │ │
│  └──────────────┘  └─────────────────────────┘ │
└────────────┬────────────────────────────────────┘
             │
             ▼
┌─────────────────────────────────────────────────┐
│              Azure Storage                       │
│  ┌──────────────┐  ┌─────────────────────────┐ │
│  │ Blob Storage │  │   Table Storage         │ │
│  │ (CSV Files)  │  │   (Metrics Cache)       │ │
│  └──────┬───────┘  └──────▲──────────────────┘ │
└─────────┼──────────────────┼───────────────────┘
          │                  │
          ▼                  │
┌─────────────────────────────────────────────────┐
│         .NET 10 Processing Service               │
│  ┌──────────────────────────────────────────┐  │
│  │  Jump Segmentation Engine                │  │
│  │  Performance Calculation Engine          │  │
│  │  Data Validation & Quality Checks        │  │
│  └──────────────────────────────────────────┘  │
└────────────┬────────────────────────────────────┘
             │
             ▼
┌─────────────────────────────────────────────────┐
│          Azure OpenAI Service                    │
│  ┌──────────────────────────────────────────┐  │
│  │  AI Analysis Agent (GPT-4)               │  │
│  │  - Performance Assessment                │  │
│  │  - Safety Recommendations                │  │
│  │  - Progression Guidance                  │  │
│  └──────────────────────────────────────────┘  │
└────────────┬────────────────────────────────────┘
             │
             ▼
┌─────────────────────────────────────────────────┐
│         PowerShell CLI Output                    │
│  - Formatted metrics display                    │
│  - AI insights and recommendations              │
│  - Markdown report generation                   │
└─────────────────────────────────────────────────┘
```

### Component Details

**1. PowerShell Module (`src/JumpMetrics.PowerShell/JumpMetrics.psm1`)**
- Cmdlet-based interface following PowerShell best practices
- Public functions dot-sourced from `Public/` directory
- Private helpers dot-sourced from `Private/` directory
- FlySight CSV parsing with error handling
- Azure Storage SDK integration
- Local caching for offline analysis
- Output formatting (tables, objects, markdown)

**2. .NET 10 Core Library (`src/JumpMetrics.Core/`)**
- Interface-driven design for testability
- Core business logic:
  - `FlySightParser` (`IFlySightParser`): CSV stream parsing
  - `JumpSegmenter` (`IJumpSegmenter`): Phase detection algorithms
  - `MetricsCalculator` (`IMetricsCalculator`): Performance computation
  - `DataValidator` (`IDataValidator`): Quality and integrity checks
- Models for jump data, segments, metrics, and AI analysis
- `ValidationResult` with errors and warnings

**3. Azure Functions (`src/JumpMetrics.Functions/`)**
- .NET 10 isolated worker process
- `AnalyzeJump` HTTP trigger (`POST /api/jumps/analyze`)
- DI registration for Core services in `Program.cs`
- Blob storage trigger support via extensions

**4. Azure OpenAI Agent**
- Specialized system prompts with skydiving domain knowledge
- Context-aware analysis using jump metrics
- Structured output for consistent recommendations
- Safety-focused reasoning with conservative guidance
- License/experience level awareness (A, B, C, D, Coach, Tandem)

**5. Azure Infrastructure (`infrastructure/main.bicep`)**
- Storage Account with Blob (`flysight-files`) and Table (`JumpMetrics`) services
- Function App on Consumption plan with .NET 10 isolated worker
- Azure OpenAI account with GPT-4 deployment
- Application Insights for monitoring
- Parameterized Bicep template with `.bicepparam` file

---

## Data Model

### FlySight 2 File Format

FlySight 2 uses a structured CSV format with a header protocol followed by `$GNSS`-prefixed data rows. This is **not** a plain CSV — the parser must handle the header protocol to correctly interpret the data section.

**File Structure:**

```
$FLYS,1                                                    # Format version identifier
$VAR,FIRMWARE_VER,v2023.09.22.2                            # Device firmware version
$VAR,DEVICE_ID,00190037484e501420353131                     # Unique hardware identifier
$VAR,SESSION_ID,88e923ed802cfc8f2ade9528                   # Recording session identifier
$COL,GNSS,time,lat,lon,hMSL,velN,velE,velD,hAcc,vAcc,sAcc,numSV   # Column definitions
$UNIT,GNSS,,deg,deg,m,m/s,m/s,m/s,m,m,m/s,                # Unit labels per column
$DATA                                                       # Marks start of data section
$GNSS,2025-09-11T17:26:18.600Z,34.7636471,-81.2017957,959.293,3.76,52.83,-8.82,293.141,1922.824,7.16,4
$GNSS,2025-09-11T17:26:18.800Z,34.7636426,-81.2016697,949.325,4.88,53.37,-7.39,137.977,863.588,3.94,4
...
```

**Header Lines:**

| Prefix | Purpose | Parser Handling |
|--------|---------|-----------------|
| `$FLYS` | Format version (currently `1`) | Validate supported version |
| `$VAR` | Key-value metadata (firmware, device ID, session ID) | Populate `JumpMetadata` |
| `$COL` | Column names for the following data type (e.g., `GNSS`) | **Use to determine column order** — do not hardcode positions |
| `$UNIT` | Unit labels for each column | Store for reference; validate expected units |
| `$DATA` | Sentinel marking the end of headers and start of data rows | Begin data parsing after this line |

**Data Row Fields (GNSS record type):**

| Column | Field | Type | Description |
|--------|-------|------|-------------|
| 1 | Record type | string | Always `$GNSS` for GPS data rows |
| 2 | `time` | ISO 8601 | Timestamp with millisecond precision (e.g., `2025-09-11T17:26:18.600Z`) |
| 3 | `lat` | double (deg) | Latitude in WGS84 decimal degrees |
| 4 | `lon` | double (deg) | Longitude in WGS84 decimal degrees |
| 5 | `hMSL` | double (m) | Height above mean sea level |
| 6 | `velN` | double (m/s) | Velocity north component |
| 7 | `velE` | double (m/s) | Velocity east component |
| 8 | `velD` | double (m/s) | Velocity down component (positive = descending, negative = ascending) |
| 9 | `hAcc` | double (m) | Horizontal position accuracy estimate |
| 10 | `vAcc` | double (m) | Vertical position accuracy estimate |
| 11 | `sAcc` | double (m/s) | Speed accuracy estimate |
| 12 | `numSV` | int | Number of satellites used in fix |

**Key Format Notes:**
- Recording rate is typically 5 Hz (200ms intervals)
- `velD` sign convention: **positive = downward**, **negative = upward** (ascending in aircraft)
- `hMSL` is meters above mean sea level, **not** above ground level (AGL). Ground elevation varies by location (e.g., ~193m MSL for the sample data location)
- Early data rows after GPS power-on have poor accuracy (`hAcc` > 100m, `numSV` < 6) due to satellite acquisition
- The `$COL` line defines the column order — different FlySight firmware versions or configurations may reorder columns

**Derived Fields (computed in `DataPoint`):**
- Horizontal speed: `Math.Sqrt(VelocityNorth² + VelocityEast²)`
- Vertical speed: `Math.Abs(VelocityDown)`
- Ground track: `Math.Atan2(VelocityEast, VelocityNorth)`
- Glide ratio: `horizontal_distance / altitude_lost`

### Sample Data Profile

The included `samples/sample-jump.csv` is a real FlySight 2 recording of a hop-n-pop (short delay) skydive with the following characteristics:

| Property | Value |
|----------|-------|
| Data points | 1,972 |
| Recording rate | 5 Hz (200ms intervals) |
| Duration | ~6 min 30 sec (17:26:18 – 17:32:48 UTC) |
| Location | ~34.76°N, 81.19°W (South Carolina) |
| Ground elevation | ~193m MSL |
| Exit altitude | ~1,910m MSL (~5,630 ft AGL) |
| Peak vertical speed | ~25 m/s (~56 mph) |

**Jump Profile Timeline:**

| Phase | Time (approx) | Altitude MSL | Characteristics |
|-------|---------------|--------------|-----------------|
| GPS acquisition | 17:26:18 – 17:26:22 | ~960m | `hAcc` > 100m, `numSV` = 4, altitude jumping erratically |
| Aircraft climb | 17:26:22 – 17:28:18 | 960m → 1,910m | `velD` negative (ascending), horizontal speed ~50 m/s (aircraft groundspeed) |
| Exit | ~17:28:18 | ~1,910m | `velD` transitions from negative to positive, horizontal speed decreasing |
| Freefall | ~17:28:20 – 17:28:35 | 1,910m → ~1,780m | `velD` accelerates to ~25 m/s, short ~15 sec delay |
| Deployment | ~17:28:35 – 17:28:38 | ~1,780m → ~1,740m | `velD` drops sharply from ~25 m/s to ~4 m/s over ~3 sec |
| Canopy flight | ~17:28:38 – 17:32:35 | 1,740m → ~193m | `velD` oscillates 0–12 m/s, steady descent, ~4 min |
| Landing | ~17:32:35 | ~193m | `velD` → 0, horizontal speed → 0, altitude plateaus at ground level |
| Post-landing | 17:32:35 – 17:32:48 | ~193m | Stationary, near-zero velocities |

This profile is a useful test case because:
- It includes **GPS acquisition noise** (poor accuracy at start)
- It includes **aircraft climb data** that must be identified and excluded from jump analysis
- The freefall is **short** (hop-n-pop), so the segmenter cannot rely solely on absolute velD thresholds
- The deployment signature is **clear** (sharp velD deceleration)
- The canopy flight is **long** relative to freefall, with variable descent rates

### Jump Data Model

Source: `src/JumpMetrics.Core/Models/`

```csharp
public class Jump
{
    public Guid JumpId { get; set; }
    public DateTime JumpDate { get; set; }
    public required string FlySightFileName { get; set; }
    public string? BlobUri { get; set; }

    public JumpMetadata Metadata { get; set; } = new();
    public List<JumpSegment> Segments { get; set; } = [];
    public JumpPerformanceMetrics? Metrics { get; set; }
    public AIAnalysis? Analysis { get; set; }
}

public class JumpSegment
{
    public SegmentType Type { get; set; } // Exit, Freefall, Deployment, Canopy, Landing
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public double StartAltitude { get; set; }
    public double EndAltitude { get; set; }
    public double Duration => (EndTime - StartTime).TotalSeconds;
    public List<DataPoint> DataPoints { get; set; } = [];
}

public class JumpMetadata
{
    public int TotalDataPoints { get; set; }
    public DateTime? RecordingStart { get; set; }
    public DateTime? RecordingEnd { get; set; }
    public double? MaxAltitude { get; set; }
    public double? MinAltitude { get; set; }

    // FlySight v2 header metadata (populated from $VAR lines)
    public string? FirmwareVersion { get; set; }     // $VAR,FIRMWARE_VER
    public string? DeviceId { get; set; }            // $VAR,DEVICE_ID
    public string? SessionId { get; set; }           // $VAR,SESSION_ID
    public int? FlySightFormatVersion { get; set; }  // $FLYS version number
}

public class JumpPerformanceMetrics
{
    public FreefallMetrics? Freefall { get; set; }
    public CanopyMetrics? Canopy { get; set; }
    public LandingMetrics? Landing { get; set; }
}

public class AIAnalysis
{
    public string OverallAssessment { get; set; } = string.Empty;
    public List<SafetyFlag> SafetyFlags { get; set; } = [];
    public List<string> Strengths { get; set; } = [];
    public List<string> ImprovementAreas { get; set; } = [];
    public string ProgressionRecommendation { get; set; } = string.Empty;
    public int SkillLevel { get; set; } // 1-10 rating
}

public class SafetyFlag
{
    public required string Category { get; set; }
    public required string Description { get; set; }
    public SafetySeverity Severity { get; set; }
}

public enum SafetySeverity { Info, Warning, Critical }
```

---

## Implementation Phases

### Phase 1: Foundation — Data Ingestion & Parsing

**Status:** Scaffolding complete. Parser and validator not yet implemented.

**Deliverables:**
- [x] Project repository structure
- [x] PowerShell module skeleton with basic cmdlets
- [x] .NET solution with Core library and Functions project
- [x] Data models and service interfaces
- [x] Unit test scaffolding (xUnit + Pester)
- [x] Real FlySight 2 sample data file (`samples/sample-jump.csv`)
- [ ] `JumpMetadata` model extension (firmware version, device ID, session ID, format version)
- [ ] `FlySightParser` implementation
- [ ] `DataValidator` implementation
- [ ] Unit tests for parser and validator

#### FlySightParser Requirements

The parser (`src/JumpMetrics.Core/Services/FlySightParser.cs`) implements `IFlySightParser.ParseAsync(Stream, CancellationToken)` and must handle the real FlySight v2 file format:

**Header Parsing:**
1. Read lines until `$DATA` sentinel is encountered
2. Parse `$FLYS,<version>` — store format version, reject unsupported versions
3. Parse `$VAR,<key>,<value>` lines — collect firmware version, device ID, session ID into a metadata object
4. Parse `$COL,GNSS,<columns...>` — build a column-name-to-index mapping. **Do not hardcode column positions.** The `$COL` line is the source of truth for field ordering
5. Parse `$UNIT,GNSS,<units...>` — store for reference, optionally validate expected units
6. Ignore any unrecognized `$` header lines gracefully (forward compatibility)

**Data Row Parsing:**
1. After `$DATA`, parse each `$GNSS,...` line using the column mapping from `$COL`
2. Map fields to `DataPoint` properties:
   - `time` → `DateTime` (parse ISO 8601 with `DateTimeOffset.Parse`, convert to UTC)
   - `lat` → `Latitude`, `lon` → `Longitude` (double)
   - `hMSL` → `AltitudeMSL` (double, meters)
   - `velN` → `VelocityNorth`, `velE` → `VelocityEast`, `velD` → `VelocityDown` (double, m/s)
   - `hAcc` → `HorizontalAccuracy`, `vAcc` → `VerticalAccuracy` (double, meters)
   - `sAcc` → `SpeedAccuracy` (double, m/s)
   - `numSV` → `NumberOfSatellites` (int)
3. Skip malformed rows with logging rather than failing the entire parse
4. Return data points ordered by timestamp
5. The parser should also return or make accessible the parsed metadata (`$VAR` values, format version)

**Edge Cases:**
- Empty stream → return empty list
- File with headers but no data rows → return empty list
- Rows with missing or extra fields → skip with warning
- Non-`$GNSS` data rows (future record types) → skip silently
- Very large files (>100k data points) → use streaming, avoid loading entire file into memory

**Test Cases to Implement:**
- Parse the real sample file and verify data point count, first/last timestamps, altitude range
- Parse a minimal valid file (header + 1 data row)
- Handle empty stream
- Handle file with headers only (no `$DATA` or no data rows after `$DATA`)
- Handle malformed data rows (missing fields, non-numeric values)
- Verify column mapping works regardless of `$COL` column order
- Verify metadata extraction (firmware version, device ID, session ID)

#### DataValidator Requirements

The validator (`src/JumpMetrics.Core/Services/Validation/DataValidator.cs`) implements `IDataValidator.Validate(IReadOnlyList<DataPoint>)`:

**Errors (IsValid = false):**
- Fewer than 10 data points (insufficient for any analysis)
- No data points at all
- All timestamps identical (no time progression)

**Warnings (IsValid = true, but flagged):**
- Data points with `hAcc` > 50m (poor GPS accuracy — typical during satellite acquisition)
- Data points with `numSV` < 6 (insufficient satellites for reliable 3D fix)
- Timestamps not monotonically increasing (out-of-order data)
- Gaps > 2 seconds between consecutive data points (missing data)
- Altitude values outside reasonable skydiving range (below -100m or above 10,000m MSL)
- `velD` magnitude > 150 m/s (physically implausible for skydiving, likely GPS error)

**Test Cases to Implement:**
- Valid dataset passes with no errors or warnings
- Empty list returns error
- Small dataset (<10 points) returns error
- Dataset with GPS acquisition noise (high `hAcc`, low `numSV`) returns warnings
- Dataset with time gaps returns warnings
- Dataset with implausible velocities returns warnings

**Success Criteria:**
- Successfully parse the real FlySight v2 sample file
- Extract all GPS coordinates, altitude, and velocity data correctly
- Handle the v2 header protocol (metadata, dynamic column ordering)
- Handle malformed or incomplete data gracefully (skip bad rows, don't crash)
- Validator correctly flags GPS acquisition noise in sample data
- Pass all unit tests

---

### Phase 2: Jump Segmentation

**Status:** Not started. Depends on Phase 1 (parser + validator).

**Deliverables:**
- [ ] `SegmentType.Aircraft` addition to enum (or handle aircraft phase as pre-jump data)
- [ ] `JumpSegmenter` implementation
- [ ] Configurable segmentation thresholds (via options pattern or constructor parameters)
- [ ] Integration tests with real FlySight data
- [ ] Unit tests with synthetic data for edge cases

#### JumpSegmenter Requirements

The segmenter (`src/JumpMetrics.Core/Services/Segmentation/JumpSegmenter.cs`) implements `IJumpSegmenter.Segment(IReadOnlyList<DataPoint>)`:

**Phase Detection Strategy:**

The segmenter must detect phase transitions from GPS and velocity data. It should use **rate-of-change and pattern detection** rather than absolute thresholds alone, because:
- Hop-n-pops may never reach terminal velocity
- Exit altitude varies widely (3,000 ft to 18,000+ ft)
- Altitude is MSL, not AGL — ground elevation varies by location

**Recommended detection approach for each phase:**

| Phase | Primary Signal | Secondary Signals | Notes |
|-------|---------------|-------------------|-------|
| **Aircraft / Pre-jump** | `velD` < 0 (ascending) OR altitude increasing over time | High horizontal speed (~40-70 m/s for aircraft), low vertical speed | Discard or label as non-jump data |
| **Exit** | `velD` transitions from ≤0 to >0 at/near peak altitude | Horizontal speed begins decreasing (no longer matching aircraft), altitude at or near maximum | Transition point, not a sustained phase |
| **Freefall** | `velD` > 0 and **accelerating** (increasing over consecutive samples) | Horizontal speed decoupled from aircraft speed, sustained descent | Use a sliding window average to smooth GPS noise. Do **not** require velD > 30 m/s — hop-n-pops may only reach 15-25 m/s |
| **Deployment** | `velD` **decelerating sharply** (large negative rate of change) | Deceleration sustained over 2-5 seconds (not a single-sample spike) | This is the most reliable transition signal — a drop from freefall speed to canopy speed |
| **Canopy** | `velD` positive, stable, moderate (typically 3-8 m/s average) | Horizontal speed moderate (5-15 m/s), steady descent pattern | Longest phase for hop-n-pops. Descent rate oscillates due to turns and control inputs |
| **Landing** | Altitude plateaus near minimum recorded altitude | `velD` → 0, horizontal speed → 0 | Detect when altitude stops decreasing and velocity approaches zero |

**Smoothing and Noise Handling:**
- Apply a sliding window average (e.g., 5-10 samples / 1-2 seconds at 5 Hz) for velD to reduce GPS noise
- Ignore single-sample spikes in velocity (GPS glitches)
- GPS acquisition data at the start of recording has very high noise — the segmenter should either skip data points where `hAcc` > some threshold or start analysis only after GPS accuracy stabilizes

**Configurable Thresholds (suggested defaults):**
- `MinFreefallVelD`: Minimum smoothed velD to consider as freefall (default: 10 m/s — covers hop-n-pops)
- `DeploymentDecelThreshold`: Rate of velD decrease that indicates deployment (e.g., >5 m/s² sustained over 2+ seconds)
- `CanopyVelDRange`: Expected velD range under canopy (default: 2-15 m/s)
- `LandingVelDThreshold`: Maximum velD to consider "on the ground" (default: 1 m/s)
- `LandingHorizontalThreshold`: Maximum horizontal speed to consider "stopped" (default: 2 m/s)
- `GpsAccuracyThreshold`: Maximum `hAcc` for data points to be used in segmentation (default: 50m)
- `SmoothingWindowSize`: Number of samples for sliding window average (default: 5)

**Edge Cases to Handle:**
- **Hop-n-pop**: Short or no freefall. May go directly from exit to deployment within seconds. Peak velD may be only 15-25 m/s
- **High pull**: Deployment at high altitude with long freefall but early pull. Similar to hop-n-pop but with more altitude loss
- **Recording starts mid-jump**: No aircraft climb data. Must detect freefall from the start of data
- **Recording ends before landing**: No landing phase. Segments should end at last available data
- **Ground-level recording only**: FlySight turned on but no jump. Should detect "no jump" and return empty segments
- **Turbulent canopy flight**: Large velD oscillations under canopy (turns, braking) should not be misidentified as freefall or deployment

**Test Cases to Implement:**
- Segment the real sample file — verify correct phase sequence and approximate transition times
- Synthetic freefall-only data (no aircraft, no canopy) — verify freefall detection
- Synthetic canopy-only data (high pull, long canopy ride) — verify canopy detection
- Flat ground recording (no jump) — verify empty or no-jump result
- Data starting mid-freefall — verify partial segmentation

---

### Phase 3: Metrics Calculation

**Status:** ✅ Complete. Fully implemented and tested.

**Deliverables:**
- [x] `MetricsCalculator` implementation
- [x] Freefall metrics calculation
- [x] Canopy flight metrics calculation
- [x] Landing metrics calculation
- [x] Unit tests with known-input/known-output scenarios

#### MetricsCalculator Requirements

The calculator (`src/JumpMetrics.Core/Services/Metrics/MetricsCalculator.cs`) implements `IMetricsCalculator.Calculate(IReadOnlyList<JumpSegment>)`:

**Freefall Metrics (from `SegmentType.Freefall` segment):**
- `AverageVerticalSpeed`: Mean of `VelocityDown` across freefall data points (m/s)
- `MaxVerticalSpeed`: Maximum `VelocityDown` value in freefall (m/s)
- `AverageHorizontalSpeed`: Mean of `HorizontalSpeed` (computed property) across freefall (m/s)
- `TrackAngle`: Average angle of horizontal travel relative to north, computed from velocity vectors (degrees)
- `TimeInFreefall`: Duration of the freefall segment (seconds)

**Canopy Metrics (from `SegmentType.Canopy` segment):**
- `DeploymentAltitude`: `StartAltitude` of the canopy segment (meters MSL)
- `AverageDescentRate`: Mean of `VelocityDown` under canopy (m/s)
- `GlideRatio`: Total horizontal distance traveled / total altitude lost during canopy flight (dimensionless). Horizontal distance calculated by summing `HorizontalSpeed * dt` between consecutive data points
- `MaxHorizontalSpeed`: Maximum `HorizontalSpeed` during canopy flight (m/s)
- `TotalCanopyTime`: Duration of canopy segment (seconds)
- `PatternAltitude`: Altitude when the jumper enters the landing pattern. Heuristic: detect when the jumper begins a sustained turn sequence below 300m AGL (optional, null if not detectable)

**Landing Metrics (from `SegmentType.Landing` segment):**
- `FinalApproachSpeed`: Average `HorizontalSpeed` over the last 10 seconds before touchdown (m/s)
- `TouchdownVerticalSpeed`: `VelocityDown` at the last data point before velocity reaches ~0 (m/s)
- `LandingAccuracy`: Distance from a target coordinate, if provided (meters, null if no target)

**Edge Cases:**
- Missing segments (e.g., no freefall in a hop-n-pop that deploys immediately) → return null for that metrics group
- Very short segments (<3 data points) → still calculate what's possible but note low confidence
- `PatternAltitude` requires knowing ground elevation (minimum recorded altitude as proxy)

**Test Cases Implemented:**
- ✅ Calculate metrics from synthetic jump segments with known values
- ✅ Verify freefall averages and maximums against manually computed values
- ✅ Verify glide ratio calculation with known horizontal distance and altitude loss (200m / 100m = 2.0)
- ✅ Verify horizontal speed calculation (sqrt(3² + 4²) = 5.0)
- ✅ Handle missing freefall segment (null FreefallMetrics)
- ✅ Handle empty segment list
- ✅ Handle short segments (<3 data points)
- ✅ Handle zero altitude loss (glide ratio = 0)
- ✅ Handle landing segments shorter than 10 seconds
- ✅ Multi-phase jump scenarios (freefall + canopy + landing)

**Implementation Highlights:**
- All 14 unit tests passing
- Robust edge case handling (null segments, empty data, short segments)
- Accurate glide ratio calculation via horizontal distance integration
- Smart approach speed calculation (10-second window with fallback)
- Pattern altitude detection using turn rate heuristics below 300m AGL
- Ready for integration with JumpSegmenter output (Phase 2)

---

### Phase 4: Azure Infrastructure & Integration

**Status:** Bicep templates and CI/CD complete. Function integration not started.

**Deliverables:**
- [x] Bicep deployment template (`infrastructure/main.bicep`)
- [x] Azure Function App scaffolding (isolated worker, HTTP trigger)
- [x] GitHub Actions CI pipeline (build + test)
- [ ] DI registration of Core services in Functions `Program.cs`
- [ ] `AnalyzeJumpFunction` implementation (accept CSV upload, run pipeline, return JSON)
- [ ] Azure Storage integration (upload blob, store metrics in Table storage)
- [ ] Storage account integration from PowerShell module
- [ ] Function App end-to-end invocation testing

**Success Criteria:**
- Infrastructure deploys successfully via IaC
- Function App accepts a FlySight CSV via HTTP POST and returns segmented metrics as JSON
- Storage accounts accessible from PowerShell module
- CI pipeline runs on push/PR to main branch

---

### Phase 5: AI Agent Integration

**Status:** Not started. Depends on Phases 1-3 (structured metrics data needed as AI input).

**Deliverables:**
- [ ] `IAIAnalysisService` implementation
- [ ] Azure OpenAI client configuration and service registration
- [ ] System prompt engineering with skydiving domain knowledge
- [ ] Structured output parsing (JSON → `AIAnalysis` model)
- [ ] Safety flag detection logic (low pulls, aggressive canopy flight, etc.)
- [ ] Prompt engineering and testing

**Success Criteria:**
- AI generates relevant, safety-focused insights
- Analysis includes specific, actionable recommendations
- Response time <10 seconds for typical jump
- Handles various jump types (belly, freefly, wingsuit, hop-n-pop, etc.)
- Safety flags correctly identify concerning patterns

---

### Phase 6: CLI Polish & Documentation

**Status:** Cmdlet signatures and help text exist. Implementation not started.

**Deliverables:**
- [ ] `ConvertFrom-FlySightCsv.ps1` implementation (PowerShell-native parsing for local/offline use)
- [ ] `Import-FlySightData` implementation (parse locally, optionally upload to Azure)
- [ ] `Get-JumpAnalysis` implementation (retrieve AI analysis)
- [ ] `Get-JumpMetrics` implementation (display calculated metrics)
- [ ] `Get-JumpHistory` implementation (list processed jumps from Table storage)
- [ ] `Export-JumpReport` implementation (generate markdown report)
- [ ] Example usage scripts
- [ ] Contributing guidelines

**Success Criteria:**
- All cmdlets have `-Help` documentation (already done)
- Example workflows documented
- Users can install and use without prior knowledge
- Code follows PowerShell best practices

---

## Backlog

### High Priority
- [ ] Error handling and logging throughout all components
- [ ] Configuration management (appsettings, environment variables)
- [ ] Cost optimization (Azure Function consumption plan, caching)
- [ ] Security: API authentication, storage access controls
- [ ] Performance benchmarking with large datasets

### Medium Priority
- [ ] Batch processing support (multiple jumps in one command)
- [ ] Jump comparison features ("compare this jump to my last 10")
- [ ] Export formats: PDF reports, Excel workbooks
- [ ] Integration with popular logbook services
- [ ] Equipment tracking (canopy type, wing loading, etc.)

### Low Priority / Future Enhancements
- [ ] Web UI dashboard (Blazor or React)
- [ ] Real-time GPS tracking during jumps
- [ ] Mobile app integration
- [ ] Social features and jump sharing
- [ ] Competition scoring automation
- [ ] Video overlay generation
- [ ] Weather data integration
- [ ] Advanced analytics (trend analysis, predictive modeling)
- [ ] Multi-user support and team features
- [ ] Wingsuit-specific analysis
- [ ] Formation skydiving pattern recognition

---

## Build & Test

```powershell
# Restore, build, and test the .NET solution
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release

# Run PowerShell Pester tests
Install-Module -Name Pester -Force -SkipPublisherCheck
Invoke-Pester -Path ./src/JumpMetrics.PowerShell/Tests -Output Detailed
```

---

## Success Metrics

**Technical Excellence:**
- 80%+ code coverage for core logic
- All Azure resources deployed via IaC
- Zero secrets in source code
- Successful CI/CD pipeline execution
- Performance: <3 seconds to process typical jump file

**Functionality:**
- Parse 100% of valid FlySight 2 files
- Accurate phase detection (95%+ accuracy)
- Meaningful AI insights for various jump types
- User-friendly CLI experience

**Portfolio Value:**
- Demonstrates PowerShell automation skills
- Shows .NET proficiency and clean architecture
- Proves Azure cloud competency
- Highlights AI/agent orchestration capability
- Unique domain application (skydiving niche)

---

## Getting Started

### Prerequisites
- PowerShell 7.5+
- .NET 10 SDK
- Azure subscription
- Azure CLI
- Git

### Initial Setup

1. **Clone Repository**
   ```powershell
   git clone https://github.com/yourusername/JumpMetricsAI.git
   cd JumpMetricsAI
   ```

2. **Install Dependencies**
   ```powershell
   dotnet restore
   ```

3. **Configure Azure Resources**
   ```powershell
   az login
   az deployment group create --resource-group jumpmetrics-rg --template-file infrastructure/main.bicep
   ```

4. **Set Environment Variables**
   ```powershell
   $env:AZURE_STORAGE_CONNECTION_STRING = "your-connection-string"
   $env:AZURE_OPENAI_ENDPOINT = "your-openai-endpoint"
   $env:AZURE_OPENAI_KEY = "your-api-key"
   ```

5. **Import PowerShell Module**
   ```powershell
   Import-Module ./src/JumpMetrics.PowerShell/JumpMetrics.psm1
   ```

6. **Test with Sample Data**
   ```powershell
   Import-FlySightData -Path ./samples/sample-jump.csv
   Get-JumpAnalysis -JumpId <returned-id>
   ```

---

## Contributing

This is a learning and portfolio project. Contributions, suggestions, and feedback are welcome! See CONTRIBUTING.md for guidelines.

---

## License

MIT License - See LICENSE file for details.

---

## Acknowledgments

- FlySight GPS project for providing excellent open-source hardware
- Skydiving community for domain expertise and safety culture
- Azure and OpenAI for cloud and AI services

---

**Project Status:** Skeleton complete. Real FlySight v2 sample data integrated. Phase 1 implementation (parser + validator) is next.

Last Updated: 2026-02-01
