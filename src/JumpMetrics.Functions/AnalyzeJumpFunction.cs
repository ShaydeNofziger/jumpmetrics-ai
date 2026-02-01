using System.Net;
using System.Text.Json;
using JumpMetrics.Core.Interfaces;
using JumpMetrics.Core.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace JumpMetrics.Functions;

public class AnalyzeJumpFunction
{
    private readonly ILogger<AnalyzeJumpFunction> _logger;
    private readonly IFlySightParser _parser;
    private readonly IDataValidator _validator;
    private readonly IJumpSegmenter _segmenter;
    private readonly IMetricsCalculator _metricsCalculator;
    private readonly IStorageService _storageService;

    public AnalyzeJumpFunction(
        ILogger<AnalyzeJumpFunction> logger,
        IFlySightParser parser,
        IDataValidator validator,
        IJumpSegmenter segmenter,
        IMetricsCalculator metricsCalculator,
        IStorageService storageService)
    {
        _logger = logger;
        _parser = parser;
        _validator = validator;
        _segmenter = segmenter;
        _metricsCalculator = metricsCalculator;
        _storageService = storageService;
    }

    [Function("AnalyzeJump")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "jumps/analyze")] HttpRequestData req,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("AnalyzeJump function triggered.");

        try
        {
            // Parse multipart form data to get the CSV file
            var (fileName, fileStream) = await ParseMultipartFormDataAsync(req, cancellationToken);

            if (string.IsNullOrEmpty(fileName) || fileStream == null)
            {
                _logger.LogWarning("No file found in request");
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteAsJsonAsync(new
                {
                    error = "No CSV file provided. Please upload a FlySight CSV file."
                }, cancellationToken);
                return badRequestResponse;
            }

            _logger.LogInformation("Processing file: {FileName}", fileName);

            // Step 1: Parse the FlySight CSV file
            IReadOnlyList<DataPoint> dataPoints;
            try
            {
                dataPoints = await _parser.ParseAsync(fileStream, cancellationToken);
                _logger.LogInformation("Parsed {Count} data points from {FileName}", dataPoints.Count, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing FlySight file {FileName}", fileName);
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteAsJsonAsync(new
                {
                    error = "Failed to parse FlySight CSV file",
                    details = ex.Message
                }, cancellationToken);
                return errorResponse;
            }

            // Step 2: Validate the data
            var validationResult = _validator.Validate(dataPoints);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Data validation failed for {FileName}: {Errors}", fileName, string.Join(", ", validationResult.Errors));
                var validationErrorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await validationErrorResponse.WriteAsJsonAsync(new
                {
                    error = "Data validation failed",
                    errors = validationResult.Errors,
                    warnings = validationResult.Warnings
                }, cancellationToken);
                return validationErrorResponse;
            }

            if (validationResult.Warnings.Count > 0)
            {
                _logger.LogWarning("Data validation warnings for {FileName}: {Warnings}", fileName, string.Join(", ", validationResult.Warnings));
            }

            // Step 3: Segment the jump
            IReadOnlyList<JumpSegment> segments;
            try
            {
                segments = _segmenter.Segment(dataPoints);
                _logger.LogInformation("Segmented jump into {Count} segments", segments.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error segmenting jump from {FileName}", fileName);
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteAsJsonAsync(new
                {
                    error = "Failed to segment jump",
                    details = ex.Message
                }, cancellationToken);
                return errorResponse;
            }

            // Step 4: Calculate metrics
            JumpPerformanceMetrics? metrics = null;
            try
            {
                metrics = _metricsCalculator.Calculate(segments);
                _logger.LogInformation("Calculated performance metrics");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating metrics from {FileName}", fileName);
                // Continue without metrics - this is not a fatal error
                _logger.LogWarning("Continuing without metrics due to calculation error");
            }

            // Step 5: Create Jump object
            var jump = new Jump
            {
                JumpId = Guid.NewGuid(),
                JumpDate = DateTime.UtcNow,
                FlySightFileName = fileName,
                Segments = segments.ToList(),
                Metrics = metrics
            };

            // Populate metadata if available
            if (dataPoints.Count > 0)
            {
                jump.Metadata.TotalDataPoints = dataPoints.Count;
                jump.Metadata.RecordingStart = dataPoints.First().Time;
                jump.Metadata.RecordingEnd = dataPoints.Last().Time;
                jump.Metadata.MaxAltitude = dataPoints.Max(dp => dp.AltitudeMSL);
                jump.Metadata.MinAltitude = dataPoints.Min(dp => dp.AltitudeMSL);
            }

            // Step 6: Upload file to blob storage
            try
            {
                fileStream.Position = 0;
                jump.BlobUri = await _storageService.UploadFlySightFileAsync(fileName, fileStream, cancellationToken);
                _logger.LogInformation("Uploaded file to blob storage: {BlobUri}", jump.BlobUri);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file to blob storage");
                // Continue without blob URI - this is not a fatal error
            }

            // Step 7: Store metrics in table storage
            try
            {
                await _storageService.StoreJumpMetricsAsync(jump, cancellationToken);
                _logger.LogInformation("Stored jump metrics in table storage for jump {JumpId}", jump.JumpId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing jump metrics in table storage");
                // Continue - we'll still return the jump data
            }

            // Step 8: Return the Jump object as JSON
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                jumpId = jump.JumpId,
                jumpDate = jump.JumpDate,
                fileName = jump.FlySightFileName,
                blobUri = jump.BlobUri,
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
            }, cancellationToken);

            _logger.LogInformation("Successfully processed jump {JumpId}", jump.JumpId);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing jump analysis");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new
            {
                error = "An unexpected error occurred",
                details = ex.Message
            }, cancellationToken);
            return errorResponse;
        }
    }

    private async Task<(string fileName, Stream? fileStream)> ParseMultipartFormDataAsync(
        HttpRequestData req,
        CancellationToken cancellationToken)
    {
        var contentType = req.Headers.GetValues("Content-Type").FirstOrDefault();
        if (string.IsNullOrEmpty(contentType) || !contentType.Contains("multipart/form-data"))
        {
            // Try to read as raw body stream
            var stream = new MemoryStream();
            await req.Body.CopyToAsync(stream, cancellationToken);
            stream.Position = 0;

            // Try to get filename from header or use default
            var fileName = req.Headers.GetValues("X-FileName").FirstOrDefault() ?? "uploaded-file.csv";
            return (fileName, stream);
        }

        // Parse multipart form data
        var boundary = GetBoundary(contentType);
        if (string.IsNullOrEmpty(boundary))
        {
            return (string.Empty, null);
        }

        var reader = new StreamReader(req.Body);
        var content = await reader.ReadToEndAsync(cancellationToken);

        // Simple multipart parsing (for production, consider using a proper multipart parser)
        var parts = content.Split(new[] { "--" + boundary }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var part in parts)
        {
            if (part.Contains("Content-Disposition") && part.Contains("filename="))
            {
                // Extract filename
                var fileNameMatch = System.Text.RegularExpressions.Regex.Match(part, @"filename=""([^""]+)""");
                var fileName = fileNameMatch.Success ? fileNameMatch.Groups[1].Value : "unknown.csv";

                // Extract file content (after headers)
                var contentStart = part.IndexOf("\r\n\r\n");
                if (contentStart >= 0)
                {
                    var fileContent = part.Substring(contentStart + 4).Trim();
                    var memoryStream = new MemoryStream();
                    var writer = new StreamWriter(memoryStream);
                    await writer.WriteAsync(fileContent);
                    await writer.FlushAsync();
                    memoryStream.Position = 0;
                    return (fileName, memoryStream);
                }
            }
        }

        return (string.Empty, null);
    }

    private string? GetBoundary(string contentType)
    {
        var elements = contentType.Split(' ', ';');
        var boundaryElement = elements.FirstOrDefault(e => e.Trim().StartsWith("boundary="));
        if (boundaryElement == null)
            return null;

        return boundaryElement.Split('=')[1].Trim('"');
    }
}
