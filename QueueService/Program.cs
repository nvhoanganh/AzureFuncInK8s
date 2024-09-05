using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using AzureFuncInK8s;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

string connectionString = Environment.GetEnvironmentVariable("SqlConnection");

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.AddApplicationInsights();
            loggingBuilder.SetMinimumLevel(LogLevel.Information);
        });
    })
    .Build();

host.Run();
