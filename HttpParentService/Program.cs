using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.AddApplicationInsights();
            // Set the minimum logging level to Information
            loggingBuilder.SetMinimumLevel(LogLevel.Information);
        });
    })
    .Build();

host.Run();
