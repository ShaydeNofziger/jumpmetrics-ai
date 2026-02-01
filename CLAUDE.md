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

### FlySight 2 CSV Format

**Key Fields:**
- `time`: ISO 8601 timestamp with millisecond precision
- `lat`, `lon`: GPS coordinates (WGS84)
- `hMSL`: Height above mean sea level (meters)
- `velN`, `velE`, `velD`: Velocity components (m/s)
- `hAcc`, `vAcc`: Horizontal and vertical accuracy (meters)
- `sAcc`: Speed accuracy (m/s)
- `numSV`: Number of satellites

**Derived Fields (computed in `DataPoint`):**
- Horizontal speed: `Math.Sqrt(VelocityNorth² + VelocityEast²)`
- Vertical speed: `Math.Abs(VelocityDown)`
- Ground track: `Math.Atan2(VelocityEast, VelocityNorth)`
- Glide ratio: `horizontal_distance / altitude_lost`

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

### Phase 1: Foundation
**Deliverables:**
- [x] Project repository structure
- [x] PowerShell module skeleton with basic cmdlets
- [x] .NET solution with Core library and Functions project
- [x] Data models and service interfaces
- [x] Unit test scaffolding (xUnit + Pester)
- [x] Sample FlySight data file for testing
- [ ] FlySight 2 CSV parser implementation
- [ ] Basic data validation logic

**Success Criteria:**
- Successfully parse FlySight 2 CSV files
- Extract GPS coordinates, altitude, and velocity data
- Handle malformed or incomplete data gracefully
- Pass all unit tests with 80%+ code coverage

### Phase 2: Jump Segmentation
**Deliverables:**
- [ ] Jump segmentation algorithms
- [ ] Phase detection logic (exit, freefall, deployment, canopy, landing)
- [ ] Configurable thresholds for segment boundaries
- [ ] Visualization helpers for debugging segments
- [ ] Integration tests with real FlySight data

**Success Criteria:**
- Accurately detect all jump phases in test data
- Handle edge cases (cutaways, hop-n-pops, high pulls)
- Segment boundaries align with expected times ±5 seconds
- No false positives for multi-jump files

### Phase 3: Metrics Calculation
**Deliverables:**
- [ ] Freefall metrics calculator
- [ ] Canopy flight metrics calculator
- [ ] Landing metrics calculator
- [ ] Performance data models
- [ ] Metrics export to JSON/CSV

**Success Criteria:**
- Calculate all defined metrics accurately
- Validate calculations against known jump performances
- Handle incomplete data segments
- Performance: Process typical jump file (<5000 points) in <2 seconds

### Phase 4: Azure Infrastructure
**Deliverables:**
- [x] Bicep deployment template (`infrastructure/main.bicep`)
- [x] Azure Function App scaffolding (isolated worker, HTTP trigger)
- [x] GitHub Actions CI pipeline (build + test)
- [ ] Azure Storage Account deployment and validation
- [ ] Storage account integration from PowerShell module
- [ ] Function App end-to-end invocation testing

**Success Criteria:**
- Infrastructure deploys successfully via IaC
- Storage accounts accessible from PowerShell module
- Function App responds to test invocations
- CI pipeline runs on push/PR to main branch

### Phase 5: AI Agent Integration
**Deliverables:**
- [ ] Azure OpenAI service setup
- [ ] GPT-4 deployment configuration
- [ ] System prompts for skydiving analysis
- [ ] AI agent orchestration service
- [ ] Prompt engineering and testing
- [ ] Response parsing and structured output

**Success Criteria:**
- AI generates relevant, safety-focused insights
- Analysis includes specific recommendations
- Response time <10 seconds for typical jump
- Handles various jump types (belly, freefly, wingsuit, etc.)

### Phase 6: CLI Polish & Documentation
**Deliverables:**
- [ ] Complete PowerShell cmdlet implementations
- [ ] Help documentation for all cmdlets
- [ ] Markdown report generation
- [ ] Example usage scripts
- [ ] Architecture documentation
- [ ] Contributing guidelines

**Success Criteria:**
- All cmdlets have `-Help` documentation
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

**Project Status:** Skeleton complete. Implementation in progress.

Last Updated: 2026-02-01
