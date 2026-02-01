using Azure.Data.Tables;
using Azure.Storage.Blobs;
using JumpMetrics.Core.Interfaces;
using JumpMetrics.Core.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace JumpMetrics.Functions.Services;

public class AzureStorageService : IStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly TableServiceClient _tableServiceClient;
    private readonly ILogger<AzureStorageService> _logger;
    private const string BlobContainerName = "flysight-files";
    private const string TableName = "JumpMetrics";

    public AzureStorageService(
        BlobServiceClient blobServiceClient,
        TableServiceClient tableServiceClient,
        ILogger<AzureStorageService> logger)
    {
        _blobServiceClient = blobServiceClient;
        _tableServiceClient = tableServiceClient;
        _logger = logger;
    }

    public async Task<string> UploadFlySightFileAsync(string fileName, Stream fileStream, CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(BlobContainerName);
            await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

            var blobName = $"{Guid.NewGuid()}/{fileName}";
            var blobClient = containerClient.GetBlobClient(blobName);

            fileStream.Position = 0;
            await blobClient.UploadAsync(fileStream, overwrite: true, cancellationToken: cancellationToken);

            _logger.LogInformation("Uploaded FlySight file {FileName} to blob storage as {BlobName}", fileName, blobName);
            return blobClient.Uri.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading FlySight file {FileName} to blob storage", fileName);
            throw;
        }
    }

    public async Task StoreJumpMetricsAsync(Jump jump, CancellationToken cancellationToken = default)
    {
        try
        {
            var tableClient = _tableServiceClient.GetTableClient(TableName);
            await tableClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

            var entity = new TableEntity(
                partitionKey: jump.JumpDate.ToString("yyyy-MM"),
                rowKey: jump.JumpId.ToString())
            {
                { "JumpDate", jump.JumpDate },
                { "FlySightFileName", jump.FlySightFileName },
                { "BlobUri", jump.BlobUri },
                { "MetricsJson", JsonSerializer.Serialize(jump.Metrics) },
                { "MetadataJson", JsonSerializer.Serialize(jump.Metadata) },
                { "SegmentsJson", JsonSerializer.Serialize(jump.Segments) },
                { "AnalysisJson", jump.Analysis != null ? JsonSerializer.Serialize(jump.Analysis) : null }
            };

            await tableClient.UpsertEntityAsync(entity, cancellationToken: cancellationToken);
            _logger.LogInformation("Stored jump metrics for jump {JumpId} in table storage", jump.JumpId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing jump metrics for jump {JumpId} in table storage", jump.JumpId);
            throw;
        }
    }

    public async Task<Jump?> GetJumpMetricsAsync(Guid jumpId, CancellationToken cancellationToken = default)
    {
        try
        {
            var tableClient = _tableServiceClient.GetTableClient(TableName);

            // Query all partitions for the given jumpId
            var query = tableClient.QueryAsync<TableEntity>(
                filter: $"RowKey eq '{jumpId}'",
                cancellationToken: cancellationToken);

            await foreach (var entity in query)
            {
                return MapEntityToJump(entity);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving jump metrics for jump {JumpId} from table storage", jumpId);
            throw;
        }
    }

    public async Task<IReadOnlyList<Jump>> ListJumpsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var tableClient = _tableServiceClient.GetTableClient(TableName);
            var jumps = new List<Jump>();

            var query = tableClient.QueryAsync<TableEntity>(cancellationToken: cancellationToken);

            await foreach (var entity in query)
            {
                var jump = MapEntityToJump(entity);
                if (jump != null)
                {
                    jumps.Add(jump);
                }
            }

            return jumps.OrderByDescending(j => j.JumpDate).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing jumps from table storage");
            throw;
        }
    }

    private Jump? MapEntityToJump(TableEntity entity)
    {
        try
        {
            var jump = new Jump
            {
                JumpId = Guid.Parse(entity.RowKey),
                JumpDate = entity.GetDateTime("JumpDate") ?? DateTime.UtcNow,
                FlySightFileName = entity.GetString("FlySightFileName") ?? string.Empty,
                BlobUri = entity.GetString("BlobUri")
            };

            var metadataJson = entity.GetString("MetadataJson");
            if (!string.IsNullOrEmpty(metadataJson))
            {
                jump.Metadata = JsonSerializer.Deserialize<JumpMetadata>(metadataJson) ?? new JumpMetadata();
            }

            var metricsJson = entity.GetString("MetricsJson");
            if (!string.IsNullOrEmpty(metricsJson))
            {
                jump.Metrics = JsonSerializer.Deserialize<JumpPerformanceMetrics>(metricsJson);
            }

            var segmentsJson = entity.GetString("SegmentsJson");
            if (!string.IsNullOrEmpty(segmentsJson))
            {
                jump.Segments = JsonSerializer.Deserialize<List<JumpSegment>>(segmentsJson) ?? [];
            }

            var analysisJson = entity.GetString("AnalysisJson");
            if (!string.IsNullOrEmpty(analysisJson))
            {
                jump.Analysis = JsonSerializer.Deserialize<AIAnalysis>(analysisJson);
            }

            return jump;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error mapping table entity to Jump object");
            return null;
        }
    }
}
