# JumpMetrics.LocalProcessor

A command-line console application for processing FlySight 2 GPS data locally.

## Purpose

This utility provides a standalone way to process FlySight CSV files through the complete JumpMetrics pipeline without requiring the Azure Functions runtime. It's useful for:

- Local development and testing
- Offline analysis
- Debugging the processing pipeline
- Generating reports without deploying to Azure

## Features

- Parses FlySight 2 CSV files
- Validates GPS data quality
- Segments jumps into phases (Aircraft, Exit, Freefall, Deployment, Canopy, Landing)
- Calculates performance metrics
- Outputs structured JSON data

## Usage

```bash
dotnet run --project src/JumpMetrics.LocalProcessor/JumpMetrics.LocalProcessor.csproj -- <path-to-csv>
```

### Example

```bash
# Process the sample jump data
dotnet run --project src/JumpMetrics.LocalProcessor/JumpMetrics.LocalProcessor.csproj -- samples/sample-jump.csv

# Or after building:
dotnet build src/JumpMetrics.LocalProcessor/JumpMetrics.LocalProcessor.csproj --configuration Release
./src/JumpMetrics.LocalProcessor/bin/Release/net10.0/JumpMetrics.LocalProcessor samples/sample-jump.csv
```

## Output Format

The application outputs:
1. Console progress messages showing each processing step
2. A summary of detected segments
3. JSON representation of the complete jump analysis

The JSON output can be consumed by the PowerShell `Export-JumpReport` cmdlet to generate markdown reports.

## Architecture

The LocalProcessor uses the same core services as the Azure Function:

- `FlySightParser` - Parses FlySight v2 CSV format
- `DataValidator` - Validates GPS data quality
- `JumpSegmenter` - Detects jump phases using rate-of-change algorithms
- `MetricsCalculator` - Computes performance metrics

This ensures that local processing produces identical results to the cloud-deployed Azure Function.

## See Also

- `scripts/process-sample-local.ps1` - PowerShell wrapper script
- `src/JumpMetrics.PowerShell/` - PowerShell module for report generation
- `src/JumpMetrics.Functions/` - Azure Functions implementation
