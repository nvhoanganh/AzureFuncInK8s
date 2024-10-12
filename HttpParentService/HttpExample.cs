using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AzureFuncInK8s;

public class HttpExample
{
    private readonly ILogger<HttpExample> _logger;

    public HttpExample(ILogger<HttpExample> logger)
    {
        _logger = logger;
    }

    // HTTP trigger, call child service via CHILD_SERVICE_HOST env variable
    [Function("HttpExampleParent")]
    public async Task<IActionResult> RunParent([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
    {
        _logger.LogWarning($"C# HTTP trigger parent function, calling child function via host http://{Environment.GetEnvironmentVariable("CHILD_SERVICE_HOST")}");

        var httpClient = new HttpClient();

        // call the child service
        var response = await httpClient.GetAsync($"http://{Environment.GetEnvironmentVariable("CHILD_SERVICE_HOST")}/api/HttpExampleChild");
        var rsp = await response.Content.ReadAsStringAsync();
        var finalString = $"From /api/HttpExampleChild:\n\t'{rsp}'";

        return new OkObjectResult(finalString);
    }
}

