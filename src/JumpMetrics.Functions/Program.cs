using Azure.Data.Tables;
using Azure.Storage.Blobs;
using JumpMetrics.Core.Interfaces;
using JumpMetrics.Core.Services;
using JumpMetrics.Core.Services.Metrics;
using JumpMetrics.Core.Services.Segmentation;
using JumpMetrics.Core.Services.Validation;
using JumpMetrics.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        // Register JumpMetrics.Core services
        services.AddSingleton<IFlySightParser, FlySightParser>();
        services.AddSingleton<IDataValidator, DataValidator>();
        services.AddSingleton<IJumpSegmenter, JumpSegmenter>();
        services.AddSingleton<IMetricsCalculator, MetricsCalculator>();

        // Register Azure Storage clients
        var storageConnectionString = context.Configuration.GetValue<string>("AzureStorage:ConnectionString")
            ?? context.Configuration.GetValue<string>("AzureWebJobsStorage")
            ?? "UseDevelopmentStorage=true";

        services.AddSingleton(new BlobServiceClient(storageConnectionString));
        services.AddSingleton(new TableServiceClient(storageConnectionString));

        // Register storage service
        services.AddSingleton<IStorageService, AzureStorageService>();
    })
    .Build();

host.Run();
