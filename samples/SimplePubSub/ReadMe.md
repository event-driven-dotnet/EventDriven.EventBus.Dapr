# Simple PubSub Sample

Demonstrates how to use Dapr Event Bus for simple pub/sub.

## Prerequisites
- Install [Docker Desktop](https://www.docker.com/products/docker-desktop) running Linux containers.
- Install [Dapr CLI](https://docs.dapr.io/getting-started/install-dapr-cli/).
- Run `dapr init`, `dapr --version`, `docker ps`
- [Microsoft Tye](https://github.com/dotnet/tye/blob/main/docs/getting_started.md) (recommended)

## Running the Sample

1. Open a terminal at the **samples/SimplePubSub** project root.
   - Run the following command to start the publisher and subscriber projects using Tye.

    ```
    tye run
    ```

2. Open the Tye dashboard.
   - Browse to http://localhost:8000/
   - Verify that **publisher** and **subscriber** apps are running.

3. Open a browser at http://localhost:5252/weatherforecast
   - Refresh the page every few seconds to see a new set of values.

## Debugging with Tye and the IDE of your choice

1. Open the publisher and subscriber projects in your IDE.
   - Set breakpoints where you want to pause code execution.
2. Attach to the Publisher or Subscriber processes.

> If you need to debug startup code, start Tye with the `debug` flag.
> Specify * for debugging all projects, or specify a project name.
> Code execution will begin when you attach to the process.

```
tye run --debug *
```
```
tye run --debug Backend
```