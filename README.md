# Deploy Sample Azure Function in Docker and install New Relic APM agent

## Setup sample Azure Function in Docker

- Follow [Link](https://learn.microsoft.com/en-us/azure/azure-functions/functions-deploy-container?tabs=docker%2Cbash%2Cazure-cli&pivots=programming-language-csharp) to setup local Azure function in docker
- Run it locally using `func start` and make sure you can send test request via the `test.rest` file
- Change the AuthorizationLevel to `AuthorizationLevel.Anonymous` inside the `HttpExample.cs` file
- Build the docker image locally using `❯ docker build --tag nvhoanganh1909/azurefuncdockernewrelic:v1.0.0 .` (replace `nvhoanganh1909` with your docker HUB account Id)
- Run the Az funcion inside docker using command `docker run -p 8080:80 -it nvhoanganh1909/azurefuncdockernewrelic:v1.0.0`