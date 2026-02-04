using System.Text.Json;
using JumpMetrics.Core.Interfaces;
using JumpMetrics.Core.Models;
using JumpMetrics.Core.Services;
using JumpMetrics.Core.Services.Metrics;
using JumpMetrics.Core.Services.Processing;
using JumpMetrics.Core.Services.Segmentation;
using JumpMetrics.Core.Services.Validation;

namespace JumpMetrics.CLI;

class Program
{
    static async Task<int> Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("Usage: JumpMetrics.CLI <path-to-flysight-csv>");
            return 1;
        }

        var filePath = args[0];
        
        if (!File.Exists(filePath))
        {
            Console.Error.WriteLine($"File not found: {filePath}");
            return 1;
        }

        try
        {
            // Create services
            IFlySightParser parser = new FlySightParser();
            IDataValidator validator = new DataValidator();
            IJumpSegmenter segmenter = new JumpSegmenter(new SegmentationOptions());
            IMetricsCalculator metricsCalculator = new MetricsCalculator();

            // Create processor
            var processor = new LocalJumpProcessor(parser, validator, segmenter, metricsCalculator);

            // Process the jump
            var jump = await processor.ProcessJumpAsync(filePath);

            // Output as JSON to stdout
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(jump, options);
            Console.WriteLine(json);

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error processing jump: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            return 1;
        }
    }
}
