# Deploy Sample Azure Function in Docker and install New Relic APM agent

## Setup sample Azure Function in Docker

- Follow [Link](https://learn.microsoft.com/en-us/azure/azure-functions/functions-deploy-container?tabs=docker%2Cbash%2Cazure-cli&pivots=programming-language-csharp) to setup local Azure function in docker
- Run it locally using `func start` and make sure you can send test request via the `test.rest` file
- Change the AuthorizationLevel to `AuthorizationLevel.Anonymous` inside the `HttpExample.cs` file
- Build the docker image locally using `❯ docker build --tag nvhoanganh1909/azurefuncdockernewrelic:v1.0.0 .` (replace `nvhoanganh1909` with your docker HUB account Id)
- Run the Az funcion inside docker using command `docker run -p 8080:80 -it nvhoanganh1909/azurefuncdockernewrelic:v1.0.0`

## Adding new Relic agent (via docker)

- add this to your Dockerfile, after the base image

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
NEW_RELIC_LICENSE_KEY=<LICENSEKEY> \
NEW_RELIC_APP_NAME="Azure Function in Docker Sample"
```
- run the build again, increase the tag version `docker build --tag nvhoanganh1909/azurefuncdockernewrelicwithapm:v1.0.0 .`
- run the new docker version (with New Relic agent this time) `docker run -p 8080:80 -it nvhoanganh1909/azurefuncdockernewrelicwithapm:v1.0.0`