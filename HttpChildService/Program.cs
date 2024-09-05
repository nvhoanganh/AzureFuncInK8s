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
            // Set the minimum logging level to Information
            loggingBuilder.SetMinimumLevel(LogLevel.Information);
        });
        services.AddDbContext<FuncDbContext>(
          options => SqlServerDbContextOptionsExtensions.UseSqlServer(options, connectionString, b =>
            {
                // additional config goes here
            }));
    })
    .Build();

host.Run();
