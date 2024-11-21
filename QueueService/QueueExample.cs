using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using NewRelic;
using NewRelic.Api.Agent;

namespace AzureFuncInK8s;

public class QueueMessagePayload
{
    public string message { get; set; }
    public Dictionary<string, string> headers { get; set; }
}

public class QueueExample
{
    private readonly ILogger<QueueExample> _logger;

    public QueueExample(ILogger<QueueExample> logger)
    {
        _logger = logger;
    }


    // HTTP triggered function which send message to a child service via a storage queue
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

