# Simple PubSub Sample

Demonstrates how to use Dapr Event Bus for simple pub/sub.

## Prerequisites
- Install [Docker Desktop](https://www.docker.com/products/docker-desktop) running Linux containers.
- Install [Dapr CLI](https://docs.dapr.io/getting-started/install-dapr-cli/).
- Run `dapr init`, `dapr --version`, `docker ps`
- Run `dapr dashboard` from a terminal, then browse to http://localhost:8080.
- [Dapr Visual Studio Extension](https://github.com/microsoft/vscode-dapr) (for debugging).

## Running the Sample

1. Open a terminal at the **Subscriber** project root.
   - Run the following Dapr command to start the subscriber.

    ```
    dapr run --app-id subscriber --app-port 5252 --components-path ../dapr/components -- dotnet run
    ```

2. Open a terminal at the **Publisher** project root.
   - Run the following Dapr command to start the publisher.

    ```
    dapr run --app-id publisher --components-path ../dapr/components -- dotnet run
    ```

3. Open a browser at http://localhost:5252/weatherforecast
   - Refresh the page every few seconds to see a new set of values.
   - Note log output to each terminal.

## Debugging with Visual Studio Code

1. Open the publisher and subscriber in separate VS Code instances.
   - Set breakpoints where you want to pause code execution.
2. Press Ctrl+P, enter `Dapr` and select `Scaffold Dapr Tasks`.
   - Select .NET Core Launch (web)
   - Enter the app id (frontend, backend, weather-generator).
   - Enter the port number.
3. On the VS Code Debug tab select the `with Dapr` option.
   - Start debugging the subscriber first by  clicking the Run button or pressing F5.
4. Browse to http://localhost:5000/weatherforecast
   - Refresh every few seconds to see a new set of values.