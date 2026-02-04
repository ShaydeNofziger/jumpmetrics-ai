# JumpMetrics AI - Examples

This directory contains example scripts demonstrating how to use JumpMetrics AI for analyzing FlySight 2 GPS data locally.

## Example Scripts

### 01-local-analysis.ps1
Complete local jump analysis workflow:
- Parses FlySight 2 CSV files
- Validates GPS data
- Segments jump into phases (aircraft, exit, freefall, deployment, canopy, landing)
- Calculates performance metrics
- Generates markdown report

**Usage:**
```powershell
# From repository root
pwsh examples/01-local-analysis.ps1

# Or customize the script for your own files
Import-Module ./src/JumpMetrics.PowerShell/JumpMetrics.psm1
$jump = Import-FlySightData -Path ./path/to/your-jump.csv
Get-JumpMetrics -JumpData $jump
Export-JumpReport -JumpData $jump -OutputPath ./reports/my-jump.md
```

**Requirements:**
- PowerShell 7.5+
- .NET 10 SDK
- Build the project first: `dotnet build src/JumpMetrics.CLI/JumpMetrics.CLI.csproj --configuration Release`

## Processing Pipeline

1. **Parsing**: Reads FlySight 2 CSV with header protocol ($FLYS, $VAR, $COL, $DATA)
2. **Validation**: Checks data quality, GPS accuracy, satellite count
3. **Segmentation**: Detects jump phases using rate-of-change algorithms
4. **Metrics Calculation**: Computes freefall, canopy, and landing performance

## Local Storage

Processed jumps can be saved to local storage in JSON format:

```powershell
# Save to default location (~/.jumpmetrics/jumps/)
Import-FlySightData -Path ./jump.csv -SaveToStorage

# Save to custom location
Import-FlySightData -Path ./jump.csv -SaveToStorage -StoragePath "C:\MyJumps"

# Retrieve from storage
Get-JumpHistory -StoragePath "~/.jumpmetrics/jumps"
Get-JumpMetrics -JumpId "guid-here" -StoragePath "~/.jumpmetrics/jumps"
```

## AI Analysis (Optional)

AI-powered analysis requires Azure OpenAI configuration:

```powershell
$jump = Import-FlySightData -Path ./jump.csv
Get-JumpAnalysis -JumpData $jump -GenerateWithAI \
    -OpenAIEndpoint "https://your-endpoint.openai.azure.com" \
    -OpenAIKey "your-api-key" \
    -OpenAIDeployment "gpt-4"
```

**Note**: AI analysis is optional. Basic jump processing (parsing, segmentation, metrics) works entirely offline without any cloud services.

## Output Format

Reports are generated in Markdown format with:
- Jump metadata (date, device, firmware)
- Recording details (data points, time range, altitude range)
- Jump segments (phase durations, altitude loss)
- Performance metrics (freefall speed, glide ratio, landing speed)
- AI analysis (if generated): safety flags, strengths, recommendations

See `reports/local-analysis-report.md` for a sample report.
