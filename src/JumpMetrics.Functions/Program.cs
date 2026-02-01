using JumpMetrics.Core.Configuration;
using JumpMetrics.Core.Interfaces;
using JumpMetrics.Core.Services.AI;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        // Configure Azure OpenAI options
        services.Configure<AzureOpenAIOptions>(
            context.Configuration.GetSection(AzureOpenAIOptions.SectionName));

        // Register JumpMetrics.Core services
        services.AddScoped<IAIAnalysisService, AIAnalysisService>();
    })
    .Build();

host.Run();
