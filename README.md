# Azure Function in Docker + New Relic .NET APM agent instrumentation

## Setup sample Azure Function in Docker

- Follow [Link](https://learn.microsoft.com/en-us/azure/azure-functions/functions-deploy-container?tabs=docker%2Cbash%2Cazure-cli&pivots=programming-language-csharp) to setup local Azure function in docker
- Run it locally using `func start` and make sure you can send test request via the `test.rest` file
- Change the AuthorizationLevel to `AuthorizationLevel.Anonymous` inside the `HttpExample.cs` file
- Build the docker image locally using `❯ docker build --tag nvhoanganh1909/azurefuncdockernewrelic:v1.0.0 .` (replace `nvhoanganh1909` with your docker HUB account Id)
- Run the Az funcion inside docker using command `docker run -p 8080:80 -it nvhoanganh1909/azurefuncdockernewrelic:v1.0.0`

## Adding new Relic agent (via docker)

- add this to your Dockerfile, after the base image, replacing the `<LICENSEKEY>` with your ingest license key
```Dockerfile

# Install the agent
RUN apt-get update && apt-get install -y wget ca-certificates gnupg \
&& echo 'deb http://apt.newrelic.com/debian/ newrelic non-free' | tee /etc/apt/sources.list.d/newrelic.list \
&& wget https://download.newrelic.com/548C16BF.gpg \
&& apt-key add 548C16BF.gpg \
&& apt-get update \
&& apt-get install -y 'newrelic-dotnet-agent' \
&& rm -rf /var/lib/apt/lists/*

# Enable the agent
ENV CORECLR_ENABLE_PROFILING=1 \
CORECLR_PROFILER={36032161-FFC0-4B61-B559-F6C5D41BAE5A} \
CORECLR_NEWRELIC_HOME=/usr/local/newrelic-dotnet-agent \
CORECLR_PROFILER_PATH=/usr/local/newrelic-dotnet-agent/libNewRelicProfiler.so \
NEW_RELIC_APP_NAME="Azure Function in Docker Sample"
```

- run the build again, with different image name (`withapm`) `docker build --tag nvhoanganh1909/azurefuncdockernewrelicwithapm:v1.0.0 .`
- run the new docker version (with New Relic agent this time) `docker run -p 8080:80 -e NEW_RELIC_LICENSE_KEY=<LICENSE_KEY> -it nvhoanganh1909/azurefuncdockernewrelicwithapm:v1.0.0`
- send few test requests and you should see an new entry under Services - APM in New Relic


## Talk to Azure SQL via Entity Framework
- install `Microsoft.EntityFrameworkCore.SqlServer` and `Microsoft.Extensions.DependencyInjection`

```csharp
> Program.cs

string connectionString = Environment.GetEnvironmentVariable("SqlConnection");
var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services => {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddDbContext<FuncDbContext>(
          options => SqlServerDbContextOptionsExtensions.UseSqlServer(options, connectionString, b =>
            {
                // additional config goes here
            }));
    })
    .Build();

> DbContext.cs

using Microsoft.EntityFrameworkCore;

namespace AzureFuncInK8s;

public class FuncDbContext : DbContext
{
  public FuncDbContext(DbContextOptions<FuncDbContext> options) : base(options)
  {
  }
  public DbSet<TESTDATA> TestData { get; set; }
}

[Keyless]
public class TESTDATA
{
    public string key { get; set; }
    public string value { get; set; }
}

> HttpExample.cs
[Function("HttpExample")]
public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
{
    _logger.LogInformation("C# HTTP trigger function processed a request.");
    var httpClient = new HttpClient();
    var finalString = "";

    // talk to Chuck Norris API to get random joke
    var response = await httpClient.GetAsync("https://api.chucknorris.io/jokes/random");
    var result = JsonConvert.DeserializeObject<ChuckNorris>(
        await response.Content.ReadAsStringAsync(), new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Ignore });
    finalString = $"From External Service (Chuck Norris Joke API):\n\t'{result.value}'";

    // connect to the SQL DB using Entity Framework
    var dbrequest = await dbContext.TestData.ToListAsync();
    finalString += "\n\nFrom Azure SQL DB:\n";
    foreach (var entry in dbrequest)
    {
        finalString += $"\t{entry.key} = {entry.value}\n";
    }

    return new OkObjectResult(finalString);
}
```
- rebuild and run the docker again, make sure you specify the `SqlConnection` env variable

```bash
docker run -p 8080:80 -e NEW_RELIC_LICENSE_KEY=<LICENSE_KEY> -e SqlConnection='<DBCONNECTION>' -it nvhoanganh1909/azurefuncdockernewrelicwithapm:v1.0.0
```

## Push the docker to Docker Hub and run inside AKS
- run `docker push nvhoanganh1909/azurefuncdockernewrelicwithapm:v1.0.0`
- run the following command to deploy this function app to Azure
- Note: if you build the docker image above using ARM chip (Mac), you will need to build deploy the the Linux version by running `docker build --tag nvhoanganh1909/azurefuncdockernewrelicwithapmlinux:v1.0.0 . -f DockerfileLinux` and then push that version instead (run `docker push nvhoanganh1909/azurefuncdockernewrelicwithapmlinux:v1.0.0`)

```bash
az login
az group create --name demoazfunc --location australiaeast
az aks create --resource-group demoazfunc --name demoazfunc --node-count 1 --enable-addons http_application_routing --generate-ssh-keys
az aks get-credentials --resource-group demoazfunc --name demoazfunc
kubectl create namespace azfuncdemo

# deploy the Azfunction app
kubectl apply -f azfunctionhttp.yaml --namespace=azfuncdemo

# set license key via env variable for the child service
kubectl set env deployment/azfunchttpexample \
    NEW_RELIC_APP_NAME="Azure Function in Docker Sample" \
    NEW_RELIC_LICENSE_KEY=<YOUR_NR_INGEST_API> \
    AzureWebJobsStorage=<StorageAccount> \
    SqlConnection=<SQLConnection> \
    --namespace=azfuncdemo

# set license key via env variable for the parent service, also specify the URL for the parent to talk to the child service
kubectl set env deployment/azfunchttpexampleparent \
    NEW_RELIC_APP_NAME="Azure Function in Docker Sample - Parent" \
    NEW_RELIC_LICENSE_KEY=<YOUR_NR_INGEST_API> \
    AzureWebJobsStorage=<StorageAccount> \
    SqlConnection=<SQLConnection> \
    CHILD_SERVICE_HOST="childservice" \
    --namespace=azfuncdemo

# get the external API 
kubectl get service azfunchttpexample --watch --namespace=azfuncdemo

# call the API
curl http://EXTERNAL-IP/api/HttpExampleParent
```

## Deploy New Relic k8s monitoring with Pixie (eBPF) via Helm3
```bash

# use the guided install on the New Relic UI to get the following command

# run this command to install the new relic bundle via helm chart
set KSM_IMAGE_VERSION v2.10.0

helm repo add newrelic https://helm-charts.newrelic.com && helm repo update && \
kubectl create namespace newrelic ; helm upgrade --install newrelic-bundle newrelic/nri-bundle \
 --set global.licenseKey=....FFFFNRAL \
 --set global.cluster=NewRelicAKSDemo \
 --namespace=newrelic \
 --set newrelic-infrastructure.privileged=true \
 --set global.lowDataMode=true \
 --set kube-state-metrics.image.tag=$KSM_IMAGE_VERSION \
 --set kube-state-metrics.enabled=true \
 --set kubeEvents.enabled=true \
 --set newrelic-prometheus-agent.enabled=true \
 --set newrelic-prometheus-agent.lowDataMode=true \
 --set newrelic-prometheus-agent.config.kubernetes.integrations_filter.enabled=false \
 --set logging.enabled=true \
 --set newrelic-logging.lowDataMode=false \
 --set newrelic-pixie.enabled=true \
 --set newrelic-pixie.apiKey=px-api-.... \
 --set pixie-chart.enabled=true \
 --set pixie-chart.deployKey=px-dep-.... \
 --set pixie-chart.clusterName=NewRelicAKSDemo
```

## Autoscale the containers to handle more loads

```bash
# install k6 (https://k6.io/docs/getting-started/installation/)
brew install k6

# set the public IP so that we can hit it via the k6 load test file
set PUBLIC_IP=<Public IP>

# Run small load test for 5 minutes, 1 Virtual user sending 1 request per second
k6 run --vus 1 --duration 300s k6.js

# use New Relic and monitor the golden metrics (low response time, 0% error rate)

# Increase the load to 5 Virtual users, you will notice requests start failing
k6 run --vus 5 --duration 3000s k6.js

# while the test is running (and failing), use autoscale to increate the number of pods for both child and parent service
kubectl autoscale deployment azfunchttpexample --cpu-percent=90 --min=1 --max=10 -n azfuncdemo
kubectl autoscale deployment azfunchttpexampleparent --cpu-percent=90 --min=1 --max=5 -n azfuncdemo

# watch number of pods increase as we increase throughput
kubectl get hpa -n azfuncdemo

# notice now error rates goes back down to 0
```
