# Duplex PubSub Sample

Demonstrates how to use Dapr Event Bus for duplex pub/sub.

## Prerequisites
- Install [Docker Desktop](https://www.docker.com/products/docker-desktop) running Linux containers.
- Install [Dapr CLI](https://docs.dapr.io/getting-started/install-dapr-cli/).
- Run `dapr init`, `dapr --version`, `docker ps`
- Run `dapr dashboard` from a terminal, then browse to http://localhost:8080.
- [Dapr Visual Studio Extension](https://github.com/microsoft/vscode-dapr) (for debugging).

## Introduction

The purpose of this sample is to show you you can use the Dapr Event Bus for pub/sub with _duplex communication for long-running operations_.

This sample consists of three applications: **Frontend**, **Backend**, **WeatherGenerator**. The Frontend is a Web UI that simply retrieves weather forecasts from the Backend Web API. The Backend relies on the WeatherGenerator to create weather forecasts. But rather than invoking the generator directly and introducing tight coupling between services, the Backend pubishes a `WeatherForecastRequestedEvent` to messaging subsystem using the **Dapr Event Bus**.

The WeatherGenerator subscribes to the `WeatherForecastRequestedEvent`, creates a set of weather forecasts, then publishes a `WeatherForecastGeneratedEvent` event to the **Dapr Event Bus**. The Backend service subscribes to `WeatherForecastGeneratedEvent` so that it can obtain the new weather forecasts.

> **Note**: For demo purposes the `WeatherForecastController` in the Backend service blocks until the `WeatherForecastGeneratedEvent` is handled. In a real-world scenario you would instead subscribe this event using [SignalR](https://dotnet.microsoft.com/apps/aspnet/signalr), so that the HTTP request from the Frontend does not block.

## Running the Sample

1. Open a terminal at the **Frontend** project root.
   - Run the following Dapr command to start the frontend.

    ```
    dapr run --app-id frontend --app-port 5200 -- dotnet run
    ```

2. Open a terminal at the **WeatherGenerator** project root.
   - Run the following Dapr command to start the weather-generator.

    ```
    dapr run --app-id weather-generator --app-port 5100 -- dotnet run
    ```

3. Open a terminal at the **Backend** project root.
   - Run the following Dapr command to start the backend.

    ```
    dapr run --app-id backend --app-port 5000 -- dotnet run
    ```

4. Open a terminal and run `dapr dashboard`.
   - Browse to http://localhost:8080/
   - Verify that **frontend**, **backend** and **weather-generator** apps are running.
5. Test with the Backend app.
   - Browse to http://localhost:5000/weatherforecast
   - Refresh the browser to initiate requests.
   - There should be a 5 second latency before results are returned.
6. Test with the Frontend app.
   - Browse to http://localhost:5200/
   - Click the "Get Weather Forecasts" button.

## Debugging with Visual Studio Code

1. Open the publisher and subscriber in separate VS Code instances.
   - Set breakpoints where you want to pause code execution.
2. Press Ctrl+P, enter `Dapr` and select `Scaffold Dapr Tasks`.
   - Select .NET Core Attach
   - Enter the app id (frontend, backend, weather-generator).
   - Enter the port number.
3. On the VS Code Debug tab select the `.NET Core Attach with Dapr` option.
   - Start debugging the subscriber first by  clicking the Run button or pressing F5.
   - Select the app process (Frontend.exe, Backend.exe, WeatherGenerator.exe).
4. Browse to http://localhost:5200/
   - Click the "Get Weather Forecasts" button.
