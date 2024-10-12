using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NewRelic.Api.Agent;

namespace AzureFuncInK8s;

public class QueueMessagePayload
{
    public string message { get; set; }
    public Dictionary<string, string> headers { get; set; }
}

public class QueueTriggerExample
{
    private readonly ILogger<QueueTriggerExample> _logger;

    public QueueTriggerExample(ILogger<QueueTriggerExample> logger)
    {
        _logger = logger;
    }

    // Queue trigger, call child service via CHILD_SERVICE_HOST env variable
    // we need [Transaction] to mark it as background transaction
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

