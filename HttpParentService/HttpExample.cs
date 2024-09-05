using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using NewRelic.Api.Agent;

namespace AzureFuncInK8s;

public class QueueMessagePayload
{
    public string message { get; set; }
    public Dictionary<string, string> headers { get; set; }
}

public class HttpExample
{
    private readonly ILogger<HttpExample> _logger;

    public HttpExample(ILogger<HttpExample> logger)
    {
        _logger = logger;
    }

    // HTTP trigger, call child service via CHILD_SERVICE_HOST env variable
    [Transaction(Web = true)]
    [Function("HttpExampleParent")]
    public async Task<IActionResult> RunParent([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
    {
        _logger.LogWarning($"C# HTTP trigger parent function, calling child function via host http://{Environment.GetEnvironmentVariable("CHILD_SERVICE_HOST")}");

        IAgent agent = NewRelic.Api.Agent.NewRelic.GetAgent();
        _logger.LogWarning(JsonConvert.SerializeObject(agent.CurrentTransaction));

        var httpClient = new HttpClient();

        // call the child service
        var response = await httpClient.GetAsync($"http://{Environment.GetEnvironmentVariable("CHILD_SERVICE_HOST")}/api/HttpExampleChild");
        var rsp = await response.Content.ReadAsStringAsync();
        var finalString = $"From /api/HttpExampleChild:\n\t'{rsp}'";

        return new OkObjectResult(finalString);
    }

    // Queue trigger, call child service via CHILD_SERVICE_HOST env variable
    [Transaction]
    [Function("QueueExampleParent")]
    [QueueOutput("testoutputqueue")]
    public async Task<string> RunQueueHandler([QueueTrigger("%QUEUE_NAME%")] string queueMessage, FunctionContext context)
    {
        // Use a string array to return more than one message.
        var request = JsonConvert.DeserializeObject<QueueMessagePayload>(queueMessage);

        // https://docs.newrelic.com/docs/apm/agents/net-agent/net-agent-api/net-agent-api/#AcceptDistributedTraceHeaders
        IAgent agent = NewRelic.Api.Agent.NewRelic.GetAgent();
        ITransaction currentTransaction = agent.CurrentTransaction;
        currentTransaction.AcceptDistributedTraceHeaders(request, Getter, TransportType.Queue);
        IEnumerable<string> Getter(QueueMessagePayload msg, string key)
        {
            string value = msg.headers[key];
            if (value != null)
                _logger.LogWarning($"New Relic DT header {key} = {value}");
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
}

