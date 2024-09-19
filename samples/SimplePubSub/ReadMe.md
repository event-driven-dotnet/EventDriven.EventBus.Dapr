# Simple PubSub Sample

Demonstrates how to use Dapr Event Bus for simple pub/sub.

## Prerequisites
- Install [Docker Desktop](https://www.docker.com/products/docker-desktop) running Linux containers.
- Install [Dapr CLI](https://docs.dapr.io/getting-started/install-dapr-cli/).
- Run `dapr init`, `dapr --version`, `docker ps`
- [.NET Aspire Workload](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/setup-tooling?tabs=dotnet-cli#install-net-aspire)
  ```
  dotnet workload update
  dotnet workload install aspire
  dotnet workload list
  ```

## Running the Sample

1. Run the `https` profile of the **SimplePubSub.AppHost** project.

2. Open the Aspire dashboard.
   - Verify that **publisher** and **subscriber** apps are running.

3. Open a browser at http://localhost:5252/weatherforecast
   - Refresh the page every few seconds to see a new set of values.

