# Duplex PubSub Sample

Demonstrates how to use Dapr Event Bus for duplex pub/sub.

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

## Introduction

The purpose of this sample is to show you you can use the Dapr Event Bus for pub/sub with _duplex communication for long-running operations_.

This sample consists of three applications: **Frontend**, **Backend**, **WeatherGenerator**. The Frontend is a Web UI that simply retrieves weather forecasts from the Backend Web API. The Backend relies on the WeatherGenerator to create weather forecasts. But rather than invoking the generator directly and introducing tight coupling between services, the Backend pubishes a `WeatherForecastRequestedEvent` to messaging subsystem using the **Dapr Event Bus**.

The WeatherGenerator subscribes to the `WeatherForecastRequestedEvent`, creates a set of weather forecasts, then publishes a `WeatherForecastGeneratedEvent` event to the **Dapr Event Bus**. The Backend service subscribes to `WeatherForecastGeneratedEvent` so that it can obtain the new weather forecasts.

> **Note**: For demo purposes the `WeatherForecastController` in the Backend service blocks until the `WeatherForecastGeneratedEvent` is handled. In a real-world scenario you would instead subscribe this event using [SignalR](https://dotnet.microsoft.com/apps/aspnet/signalr), so that the HTTP request from the Frontend does not block.

## Running LocalStack 

1. *Optional:* Run [LocalStack](https://github.com/localstack/localstack) for use of [AWS SNS+SQS](https://docs.dapr.io/operations/components/setup-pubsub/supported-pubsub/setup-aws-snssqs/) with PubSub.
   - Install [AWS CLI](https://docs.aws.amazon.com/cli/latest/userguide/install-cliv2.html) (version 2 or greater).
   - [Configure](https://docs.aws.amazon.com/cli/latest/userguide/cli-chap-configure.html) AWS CLI.
     - For access key and secret, enter: test
     - Enter a valid region.

   ```
   aws configure
   ```

   - Run LocalStack using Docker.

   ```
   docker run --name localstack --rm -p 4566:4566 -p 4571:4571 -d localstack/localstack
   ```

## Running the Sample

1. Run the `https` profile of the **DuplexPubSub.AppHost** project.

2. Open the Aspire dashboard.
   - Verify that **frontend**, **backend** and **weather-generator** apps are running.
3. Test with the Backend app.
   - Browse to http://localhost:5221/weatherforecast
   - Refresh the browser to initiate requests.
   - There should be a 5 second latency before results are returned.
4. Test with the Frontend app.
   - Browse to http://localhost:5121/
   - Click the "Get Weather Forecasts" button.
