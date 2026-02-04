using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using JumpMetrics.Core.Interfaces;
using JumpMetrics.Core.Models;
using JumpMetrics.Core.Services;
using JumpMetrics.Core.Services.Metrics;
using JumpMetrics.Core.Services.Segmentation;
using JumpMetrics.Core.Services.Validation;

namespace JumpMetrics.LocalProcessor
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: JumpMetrics.LocalProcessor <path-to-csv>");
                return;
            }

            var csvPath = args[0];
            if (!File.Exists(csvPath))
            {
                Console.WriteLine($"Error: File not found: {csvPath}");
                return;
            }

            try
            {
                Console.WriteLine("JumpMetrics AI - Local Processing");
                Console.WriteLine("=================================\n");

                // Initialize services
                IFlySightParser parser = new FlySightParser();
                IDataValidator validator = new DataValidator();
                IJumpSegmenter segmenter = new JumpSegmenter();
                IMetricsCalculator metricsCalculator = new MetricsCalculator();

                // Parse
                Console.WriteLine($"[1/4] Parsing {Path.GetFileName(csvPath)}...");
                var dataPoints = await parser.ParseAsync(File.OpenRead(csvPath), CancellationToken.None);
                Console.WriteLine($"✓ Parsed {dataPoints.Count} data points");

                // Validate
                Console.WriteLine("\n[2/4] Validating data...");
                var validationResult = validator.Validate(dataPoints);
                if (!validationResult.IsValid)
                {
                    Console.WriteLine("✗ Validation failed:");
                    foreach (var error in validationResult.Errors)
                    {
                        Console.WriteLine($"  ERROR: {error}");
                    }
                    return;
                }
                Console.WriteLine("✓ Data validation passed");
                if (validationResult.Warnings.Count > 0)
                {
                    Console.WriteLine("  Warnings:");
                    foreach (var warning in validationResult.Warnings)
                    {
                        Console.WriteLine($"    ⚠ {warning}");
                    }
                }

                // Segment
                Console.WriteLine("\n[3/4] Segmenting jump...");
                var segments = segmenter.Segment(dataPoints);
                Console.WriteLine($"✓ Segmented jump into {segments.Count} phases");

                // Calculate metrics
                Console.WriteLine("\n[4/4] Calculating metrics...");
                var metrics = metricsCalculator.Calculate(segments);
                Console.WriteLine("✓ Metrics calculated");

                // Display segments
                Console.WriteLine("\nSegments:");
                foreach (var segment in segments)
                {
                    var duration = Math.Round(segment.Duration, 1);
                    var altLoss = Math.Round(segment.StartAltitude - segment.EndAltitude, 0);
                    Console.WriteLine($"  • {segment.Type}: {duration}s, {altLoss}m altitude loss");
                }

                // Create jump object
                var jump = new Jump
                {
                    JumpId = Guid.NewGuid(),
                    JumpDate = DateTime.UtcNow,
                    FlySightFileName = Path.GetFileName(csvPath),
                    Segments = segments.ToList(),
                    Metrics = metrics
                };

                // Populate metadata
                if (dataPoints.Count > 0)
                {
                    jump.Metadata.TotalDataPoints = dataPoints.Count;
                    jump.Metadata.RecordingStart = dataPoints.First().Time;
                    jump.Metadata.RecordingEnd = dataPoints.Last().Time;
                    jump.Metadata.MaxAltitude = dataPoints.Max(dp => dp.AltitudeMSL);
                    jump.Metadata.MinAltitude = dataPoints.Min(dp => dp.AltitudeMSL);
                }

                // Output JSON
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var json = JsonSerializer.Serialize(new
                {
                    jumpId = jump.JumpId,
                    jumpDate = jump.JumpDate,
                    fileName = jump.FlySightFileName,
                    metadata = jump.Metadata,
                    segments = jump.Segments.Select(s => new
                    {
                        type = s.Type.ToString(),
                        startTime = s.StartTime,
                        endTime = s.EndTime,
                        startAltitude = s.StartAltitude,
                        endAltitude = s.EndAltitude,
                        duration = s.Duration,
                        dataPointCount = s.DataPoints.Count
                    }),
                    metrics = jump.Metrics,
                    validationWarnings = validationResult.Warnings
                }, options);

                Console.WriteLine("\n=== JSON OUTPUT ===");
                Console.WriteLine(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                Environment.Exit(1);
            }
        }
    }
}
