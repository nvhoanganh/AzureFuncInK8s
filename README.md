# Deploy Sample Azure Function in Docker and install New Relic APM agent

## Setup sample Azure Function in Docker

- Follow [Link](https://learn.microsoft.com/en-us/azure/azure-functions/functions-deploy-container?tabs=docker%2Cbash%2Cazure-cli&pivots=programming-language-csharp) to setup local Azure function in docker
- Run it locally using `func start` and make sure you can send test request via the `test.rest` file
- Change the AuthorizationLevel to `AuthorizationLevel.Anonymous` inside the `HttpExample.cs` file
- Build the docker image locally using `❯ docker build --tag nvhoanganh1909/azurefuncdockernewrelic:v1.0.0 .` (replace `nvhoanganh1909` with your docker HUB account Id)
- Run the Az funcion inside docker using command `docker run -p 8080:80 -it nvhoanganh1909/azurefuncdockernewrelic:v1.0.0`

## Adding new Relic agent (via docker)

- add this to your Dockerfile, after the base image, replacing the `<LICENSEKEY>` with your ingest license key
- install NewRelic.Agent.Api package `dotnet add package NewRelic.Agent.Api --version 10.28.0`
- add 

```csharp
[Transaction(Web = true)]
[Function("HttpExample")]
public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
{
    ...
}
```

- update the Dockerfile
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
[Transaction(Web = true)]
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

# set license key via env variable
kubectl set env deployment/azfunchttpexample \
    NEW_RELIC_LICENSE_KEY=<YOUR_NR_INGEST_API> \
    AzureWebJobsStorage=<StorageAccount> \
    SqlConnection=<SQLConnection> \
    --namespace=azfuncdemo

# get the external API 
kubectl get service azfunchttpexample --watch --namespace=azfuncdemo

# call the API
curl http://EXTERNAL-IP/api/HttpExample
```


