# Scripts Directory

This directory contains utility scripts for JumpMetrics AI development and testing.

## Available Scripts

### process-sample-local.ps1

End-to-end test script that processes the sample FlySight data through the complete JumpMetrics pipeline.

**Purpose:**
- Demonstrates the full processing pipeline locally
- Validates that parsing, validation, segmentation, and metrics calculation work correctly
- Generates a comprehensive markdown report

**Usage:**
```powershell
# Process the default sample file
./scripts/process-sample-local.ps1

# Process a specific file
./scripts/process-sample-local.ps1 -SampleFile "./path/to/jump.csv" -OutputReport "./reports/custom-report.md"
```

**What it does:**
1. Imports the JumpMetrics PowerShell module
2. Loads the JumpMetrics.Core .NET assembly
3. Parses the FlySight CSV file
4. Validates the GPS data
5. Segments the jump into phases (Aircraft, Exit, Freefall, Deployment, Canopy, Landing)
6. Calculates performance metrics
7. Generates a markdown report

**Output:**
- Console output with progress and summary
- Markdown report in `reports/` directory

**Note:**
This script uses the JumpMetrics.LocalProcessor console application under the hood, which provides the same functionality as the Azure Function's processing pipeline but runs locally without requiring the Azure Functions runtime.
