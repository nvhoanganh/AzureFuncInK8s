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
public class QueueMessagePayload
{
    public string message { get; set; }
    public Dictionary<string, string> headers { get; set; }
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
        _logger.LogWarning("C# HTTP trigger function processed a request.");
        var httpClient = new HttpClient();

        // talk to Chuck Norris API to get random joke
        var response = await httpClient.GetAsync("https://api.chucknorris.io/jokes/random");
        var result = JsonConvert.DeserializeObject<ChuckNorris>(
            await response.Content.ReadAsStringAsync(), new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Ignore });
        string? finalString = $"From External Service (Chuck Norris Joke API):\n\t'{result.value}'";

        // connect to the SQL DB using Entity Framework
        var random = Guid.NewGuid().ToString();
        var dbrequest = await dbContext.TestData.Where(x => x.key.IndexOf(random) < 0).ToListAsync();
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
        _logger.LogWarning($"C# HTTP trigger parent function, calling child function via host http://{Environment.GetEnvironmentVariable("CHILD_SERVICE_HOST")}");

        IAgent agent = NewRelic.Api.Agent.NewRelic.GetAgent();
        ITransaction currentTransaction = agent.CurrentTransaction;
        _logger.LogWarning(JsonConvert.SerializeObject(currentTransaction));

        var httpClient = new HttpClient();

        // call the child service
        var response = await httpClient.GetAsync($"http://{Environment.GetEnvironmentVariable("CHILD_SERVICE_HOST")}/api/HttpExampleChild");
        var rsp = await response.Content.ReadAsStringAsync();
        var finalString = $"From /api/HttpExampleChild:\n\t'{rsp}'";

        return new OkObjectResult(finalString);
    }

    // background transaction (non Web)
    [Transaction]
    [Function("QueueExampleParent")]
    [QueueOutput("testoutputqueue")]
    public async Task<string> RunQueueHandler([QueueTrigger("%QUEUE_NAME%")] string queueMessage, FunctionContext context)
    {
        // Use a string array to return more than one message.
        var request = JsonConvert.DeserializeObject<QueueMessagePayload>(queueMessage);

        // accept the DT headers as per https://docs.newrelic.com/docs/apm/agents/net-agent/net-agent-api/net-agent-api/#AcceptDistributedTraceHeaders
        IAgent agent = NewRelic.Api.Agent.NewRelic.GetAgent();
        ITransaction currentTransaction = agent.CurrentTransaction;
        currentTransaction.AcceptDistributedTraceHeaders(request, Getter, TransportType.Queue);
        IEnumerable<string> Getter(QueueMessagePayload msg, string key)
        {
            string value = msg.headers[key];
            if (value != null)
            {
                _logger.LogWarning($"New Relic DT header {key} = {value}");
            }
            return value == null ? null : new string[] { value };
        }

        var msg = request.message;
        _logger.LogWarning($"C# Queue trigger parent function with input '{msg}' via queue name {Environment.GetEnvironmentVariable("QUEUE_NAME")}, calling child function via host http://{Environment.GetEnvironmentVariable("CHILD_SERVICE_HOST")}");

        var httpClient = new HttpClient();

        // call the child service
        var response = await httpClient.GetAsync($"http://{Environment.GetEnvironmentVariable("CHILD_SERVICE_HOST")}/api/HttpExampleChild");
        var rsp = await response.Content.ReadAsStringAsync();
        var finalString = $"Input from queue: {msg} (queueName: {Environment.GetEnvironmentVariable("QUEUE_NAME")})\nFrom /api/HttpExampleChild:\n\t'{rsp}'";

        // Queue Output messages
        return finalString;
    }

    // HTTP triggered function which send message to a child service via a storage queue
    [Transaction(Web = true)]
    [Function("AsyncViaQueue")]
    [QueueOutput("%QUEUE_NAME%")]
    public string AsyncViaQueue([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
    {
        // Use a string array to return more than one message.
        var queueName = Environment.GetEnvironmentVariable("QUEUE_NAME");
        _logger.LogWarning($"C# HTTP trigger parent function with query string '{req.Query["query"]}', sending request via queue name {queueName}");

        var payload = new QueueMessagePayload { message = $"Sent via '{queueName}' queue from HTTP trigger", headers = new Dictionary<string, string>() };

        // https://docs.newrelic.com/docs/apm/agents/net-agent/net-agent-api/net-agent-api/#InsertDistributedTraceHeaders
        IAgent agent = NewRelic.Api.Agent.NewRelic.GetAgent();
        ITransaction currentTransaction = agent.CurrentTransaction;
        _logger.LogWarning(JsonConvert.SerializeObject(currentTransaction));
        var setter = new Action<QueueMessagePayload, string, string>((carrier, key, value) =>
        {
            _logger.LogWarning($"Adding trace header '{key}' = {value} to the message before sending it off to the '{queueName}' queue");
            carrier.headers.Add(key, value);
        });
        currentTransaction.InsertDistributedTraceHeaders(payload, setter);

        var payloadMsg = JsonConvert.SerializeObject(payload);
        _logger.LogWarning($"Sending the following mesage via '{queueName}' queue:\n {payloadMsg}");
        // Queue Output messages
        return payloadMsg;
    }
}

