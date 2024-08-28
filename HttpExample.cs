using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NewRelic;
using NewRelic.Api.Agent;

namespace AzureFuncInK8s;

public class ChuckNorris
{
    public string value { get; set; }
}
public class HttpExample
{
    private readonly ILogger<HttpExample> _logger;
    private readonly FuncDbContext dbContext;

    public HttpExample(ILogger<HttpExample> logger, FuncDbContext context)
    {
        _logger = logger;
        dbContext = context;
    }

    [Transaction(Web = true)]
    [Function("HttpExampleChild")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        var httpClient = new HttpClient();

        // talk to Chuck Norris API to get random joke
        var response = await httpClient.GetAsync("https://api.chucknorris.io/jokes/random");
        var result = JsonConvert.DeserializeObject<ChuckNorris>(
            await response.Content.ReadAsStringAsync(), new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Ignore });
        string? finalString = $"From External Service (Chuck Norris Joke API):\n\t'{result.value}'";

        // connect to the SQL DB using Entity Framework
        var dbrequest = await dbContext.TestData.ToListAsync();
        finalString += "\n\nFrom Azure SQL DB:\n";
        foreach (var entry in dbrequest)
        {
            finalString += $"\t{entry.key} = {entry.value}\n";
        }

        return new OkObjectResult(finalString);
    }

    [Transaction(Web = true)]
    [Function("HttpExampleParent")]
    public async Task<IActionResult> RunParent([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
    {
        _logger.LogInformation($"C# HTTP trigger parent function, calling child function via host http://{Environment.GetEnvironmentVariable("CHILD_SERVICE_HOST")}");

        var httpClient = new HttpClient();

        // call the child service
        var response = await httpClient.GetAsync($"http://{Environment.GetEnvironmentVariable("CHILD_SERVICE_HOST")}/api/HttpExampleChild");
        var rsp = await response.Content.ReadAsStringAsync();
        var finalString = $"From /api/HttpExampleChild:\n\t'{rsp}'";

        return new OkObjectResult(finalString);
    }
}

