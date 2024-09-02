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
    // need to do this to show Information level logs
    // https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide?tabs=windows#managing-log-levels
    .ConfigureLogging(logging =>
    {
        logging.Services.Configure<LoggerFilterOptions>(options =>
        {
            LoggerFilterRule defaultRule = options.Rules.FirstOrDefault(rule => rule.ProviderName
                == "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider");
            if (defaultRule is not null)
            {
                options.Rules.Remove(defaultRule);
            }
        });
    })
    .Build();

host.Run();
