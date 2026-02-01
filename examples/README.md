# JumpMetrics AI - Usage Examples

This directory contains example scripts demonstrating various workflows with the JumpMetrics PowerShell module.

## Prerequisites

Before running these examples:

1. **Install PowerShell 7.5+**
   ```powershell
   # Check version
   $PSVersionTable.PSVersion
   ```

2. **Import the JumpMetrics module**
   ```powershell
   Import-Module ./src/JumpMetrics.PowerShell/JumpMetrics.psm1
   ```

3. **For Azure Functions examples**: Ensure your Function App is running
   - Local development: `func start` in `src/JumpMetrics.Functions/`
   - Production: Deploy to Azure and use the production URL

## Examples

### 01-local-analysis.ps1
**Purpose:** Parse and analyze FlySight CSV files offline without Azure connectivity.

**Use case:** Quick data exploration, offline analysis, or when Azure resources are not available.

**Run:**
```powershell
./examples/01-local-analysis.ps1
```

**Output:**
- Console summary of jump metadata
- Markdown report in `reports/local-analysis-report.md`

**What you'll learn:**
- How to parse FlySight 2 CSV files locally
- How to extract metadata (device ID, firmware, GPS data points)
- How to generate a basic markdown report

---

### 02-full-workflow.ps1
**Purpose:** Complete end-to-end workflow with Azure Functions for full processing.

**Use case:** Production workflow for full jump analysis including segmentation, metrics, and AI analysis.

**Run:**
```powershell
# Local Function App
./examples/02-full-workflow.ps1 -FunctionUrl "http://localhost:7071/api/jumps/analyze"

# Production Function App with authentication
./examples/02-full-workflow.ps1 `
    -FunctionUrl "https://your-app.azurewebsites.net/api/jumps/analyze" `
    -FunctionKey "your-function-key"
```

**Output:**
- Console display of metrics and AI analysis
- Comprehensive markdown report in `reports/full-analysis-{jumpId}.md`

**What you'll learn:**
- How to upload FlySight data to Azure Functions
- How to retrieve and display performance metrics
- How to access AI-powered analysis and safety recommendations
- How to generate comprehensive reports with all analysis data

**Requires:**
- Azure Function App running
- Azure Storage configured
- (Optional) Azure OpenAI configured for AI analysis

---

### 03-batch-processing.ps1
**Purpose:** Process multiple FlySight CSV files in batch mode.

**Use case:** End-of-day processing of multiple jumps, logbook bulk import, or data migration.

**Run:**
```powershell
# Local processing only
./examples/03-batch-processing.ps1 -LocalOnly

# With Azure Functions
./examples/03-batch-processing.ps1 `
    -SourceDirectory "./my-jumps" `
    -OutputDirectory "./reports/batch" `
    -FunctionUrl "http://localhost:7071/api/jumps/analyze"
```

**Parameters:**
- `SourceDirectory`: Directory containing CSV files (default: `samples/`)
- `OutputDirectory`: Where to save reports (default: `reports/batch/`)
- `LocalOnly`: Process locally without uploading
- `FunctionUrl`: Azure Function endpoint (if not using LocalOnly)
- `FunctionKey`: Function authentication key (if required)

**Output:**
- Individual markdown report for each jump
- Console summary table showing success/failure status

**What you'll learn:**
- How to process multiple files programmatically
- Error handling and logging for batch operations
- Generating structured reports for multiple jumps

---

## Common Workflows

### Workflow 1: Quick Local Check
```powershell
# Import module
Import-Module ./src/JumpMetrics.PowerShell/JumpMetrics.psm1

# Parse file locally
$jump = Import-FlySightData -Path ./samples/sample-jump.csv -LocalOnly

# View basic info
$jump.Metadata

# Generate report
Export-JumpReport -JumpData $jump -OutputPath ./my-report.md
```

### Workflow 2: Upload and Analyze
```powershell
# Import module
Import-Module ./src/JumpMetrics.PowerShell/JumpMetrics.psm1

# Upload to Azure for full processing
$result = Import-FlySightData `
    -Path ./samples/sample-jump.csv `
    -FunctionUrl "http://localhost:7071/api/jumps/analyze"

# View metrics
$result | Get-JumpMetrics

# View AI analysis
$result | Get-JumpAnalysis

# Generate comprehensive report
$result | Export-JumpReport -OutputPath ./full-report.md
```

### Workflow 3: Retrieve Historical Jump
```powershell
# Import module
Import-Module ./src/JumpMetrics.PowerShell/JumpMetrics.psm1

# List all jumps
$jumps = Get-JumpHistory -FunctionUrl "http://localhost:7071"

# Get metrics for a specific jump
Get-JumpMetrics `
    -JumpId $jumps[0].jumpId `
    -FunctionUrl "http://localhost:7071"

# Get AI analysis for a specific jump
Get-JumpAnalysis `
    -JumpId $jumps[0].jumpId `
    -FunctionUrl "http://localhost:7071"
```

## Troubleshooting

### "No data points found in file"
- **Cause:** Invalid CSV format or empty file
- **Solution:** Verify the file is a valid FlySight 2 CSV with the proper header format

### "Failed to upload to Azure Function"
- **Cause:** Function not running or network issue
- **Solution:** 
  - Check Function is running: `func start` in Functions directory
  - Verify URL is correct (should end with `/api/jumps/analyze`)
  - Check firewall/network connectivity

### "WARNING: Found X data points with poor GPS accuracy"
- **Cause:** GPS was still acquiring satellites at start of recording
- **Impact:** Early data points may be less reliable
- **Solution:** This is normal - the parser handles it automatically

### "No AI analysis available"
- **Cause:** Azure OpenAI not configured
- **Impact:** Metrics and segmentation work, but no AI insights
- **Solution:** Configure Azure OpenAI in Function App settings or use local analysis

## Output Directories

The examples create the following directory structure:

```
jumpmetrics-ai/
├── reports/
│   ├── local-analysis-report.md        # From example 01
│   ├── full-analysis-{guid}.md         # From example 02
│   └── batch/                          # From example 03
│       ├── sample-jump-report.md
│       └── ...
```

## Getting Help

Each cmdlet has detailed help documentation:

```powershell
Get-Help Import-FlySightData -Full
Get-Help Get-JumpMetrics -Examples
Get-Help Export-JumpReport -Detailed
```

For more information, see the main [README.md](../README.md) and [CLAUDE.md](../CLAUDE.md).
