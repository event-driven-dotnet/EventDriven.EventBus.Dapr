# EventDriven.EventBus.Dapr

An event bus abstraction over Dapr pub/sub.

## Introduction

[Dapr](https://dapr.io/), which stands for **D**istributed **Ap**plication **R**untime, uses a [sidecar pattern](https://docs.microsoft.com/en-us/azure/architecture/patterns/sidecar) to provide a [pub/sub abstraction](https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-overview/) over message brokers and queuing systems, including [AWS SNS+SQS](https://www.helenanderson.co.nz/sns-sqs/), [GCP Pub/Sub](https://cloud.google.com/pubsub), [Azure Events Hub](https://azure.microsoft.com/en-us/services/event-hubs/) and several [others](https://docs.dapr.io/operations/components/setup-pubsub/supported-pubsub/).

The [Dapr .NET SDK](https://github.com/dapr/dotnet-sdk) provides an API to perform pub/sub from an ASP.NET service, but it requires the application to be directly aware of Dapr. Publishers need to use `DaprClient` to publish an event, and subscribers need to decorate controller actions with the `Topic` attribute.

The purpose of the **Dapr Event Bus** project is to provide a thin abstraction layer over Dapr pub/sub so that applications may publish events and subscribe to topics _without any knowledge of Dapr_. This allows for better testability and flexibility, especially for worker services that do not natively include an HTTP stack.

## Packages
- [EventDriven.EventBus.Abstractions](https://www.nuget.org/packages/EventDriven.EventBus.Abstractions)
- [EventDriven.EventBus.Dapr](https://www.nuget.org/packages/EventDriven.EventBus.Dapr)

## Usage

1. In both the _publisher_ and _subscriber_, you need to register the **Dapr Event Bus** with DI by calling `services.AddDaprEventBus` in `Startup.ConfigureServices`.

    ```csharp
    public void ConfigureServices(IServiceCollection services)
    {
        // Add Dapr Event Bus
        services.AddDaprEventBus(Constants.DaprPubSubName);
    }
    ```

2. Define a [C# record](https://docs.microsoft.com/en-us/dotnet/csharp/tutorials/exploration/records) that extends `IntegrationEvent`. For example, the following `WeatherForecastEvent` record does so by adding a `WeatherForecasts` property.

    ```csharp
    public record WeatherForecastEvent(IEnumerable<WeatherForecast> WeatherForecasts) : IntegrationEvent;
    ```

3. In the publisher inject `IEventBus` into the constructor of a controller (Web API projects) or worker class (Worker Service projects). Then call `EventBus.PublishAsync`, passing the event you defined in step 2.

    ```csharp
    public class Worker : BackgroundService
    {
        public Worker(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Publish event
                await _eventBus.PublishAsync(new WeatherForecastEvent(weathers));

                // Pause
                await Task.Delay(5000, stoppingToken);
            }
        }
    }
    ```

4. In the subscriber create the same `IntegrationEvent` derived class as in the publisher. Then create an **event handler** that extends `IntegrationEventHandler<TIntegrationEvent>` where `TIntegrationEvent` is the event type you defined earlier.
   - Override `HandleAsync` to perform a task when an event is received.
   - For example, `WeatherForecastEventHandler` sets `WeatherForecasts` on `WeatherForecastRepository` to the `WeatherForecasts` property of `WeatherForecastEvent`.

    ```csharp
    public class WeatherForecastEventHandler : IntegrationEventHandler<WeatherForecastEvent>
    {
        private readonly WeatherForecastRepository _weatherRepo;

        public WeatherForecastEventHandler(WeatherForecastRepository weatherRepo)
        {
            _weatherRepo = weatherRepo;
        }

        public override Task HandleAsync(WeatherForecastEvent @event)
        {
            _weatherRepo.WeatherForecasts = @event.WeatherForecasts;
            return Task.CompletedTask;
        }
    }
    ```

5. Lastly, in `Startup.Configure` in `app.UseEndpoints` call `endpoints.MapDaprEventBus`, passing an action that subscribes to `DaprEventBus` events with one or more event handlers.
   - Also call `app.UseRouting`, `app.UseCloudEvents`, `endpoints.MapSubscribeHandler`.
   - Make sure to add parameters to `Startup.Configure` to inject each handler you wish to use.
   - For example, to add the weather forecast handler, you must add a `WeatherForecastEventHandler` parameter to the `Configure` method.

    ```csharp
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env,
        WeatherForecastEventHandler forecastEventHandler)
    {
        app.UseRouting();
        app.UseCloudEvents();
        app.UseEndpoints(endpoints =>
        {
            // Map SubscribeHandler and DapEventBus
            endpoints.MapSubscribeHandler();
            endpoints.MapDaprEventBus(eventBus =>
            {
                // Subscribe with a handler
                eventBus.Subscribe(forecastGeneratedEventHandler);
            });
        });
    }
    ```

## Samples

The **samples** folder contains two sample applications which use the Dapr Event Bus: **SimplePubSub** and **DuplexPubSub**.

1. The **SimplePubSub** sample contains two projects: **Publisher** and **Subscriber**. Every 5 seconds the _Publisher_ creates a new set of weather forecasts and publishes them to the event bus. The _Subscriber_ subscribes to the event by setting the `WeatherForecasts` property of `WeatherForecastRepository`, which is returned by the `Get` method of `WeatherForecastController`.
2. The **DuplexPubSub** sample contains four projects: **Frontend**, **Backend**, **WeatherGenerator** and **Common**. The _Backend_ publishes a `WeatherForecastRequestedEvent` to the event bus in the `Get` method of the `WeatherForecastController`. The _WebGenerator_ handles the event by creating a set of weather forecasts and publishing them to the event bus with a `WeatherForecastGeneratedEvent`, which is handled by the _Backend_ by setting the `WeatherForecasts` property of the `WeatherForecastRepository`, so that new weather forecasts are returned by the `WeatherForecastController`. The _Frontend_ initiates the pub/sub process by using an `HttpClient` to call the _Backend_ when the user clicks the "Get Weather Forecasts" button.

## Dapr Event Bus Packages

The Dapr Event Bus consists of two NuGet packages: **EventDriven.EventBus.Abstractions** and **EventDriven.EventBus.Dapr**.

### EventDriven.EventBus.Abstractions

The **EventDriven.EventBus.Abstractions** package includes interfaces and abstract classes which provide an abstraction layer for interacting with any messsaging subsystem. This allows you to potentially exchange the [Dapr](https://dapr.io/) implementation with another one, such as [NServiceBus](https://particular.net/nservicebus) or [MassTransit](https://masstransit-project.com/), _without altering application code_.

This package contains an `IEventBus` interface implemented by an `EventBus` abstract class.

```csharp
public interface IEventBus
{
    Dictionary<string, List<IIntegrationEventHandler>> Topics { get; }

    void Subscribe(
        IIntegrationEventHandler handler,
        string topic = null,
        string prefix = null);

    void UnSubscribe(
        IIntegrationEventHandler handler,
        string topic = null,
        string prefix = null);

    Task PublishAsync<TIntegrationEvent>(
        TIntegrationEvent @event,
        string topic = null,
        string prefix = null)
        where TIntegrationEvent : IIntegrationEvent;
}
```

When a subscriber calls `Subscribe`, it passes a class that extends `IntegrationEventHandler`, which implements `IIntegrationEventHandler`. The event handler is added to a topic which can have one more handlers. A topic name may be specified explicitly, as well as a prefix which may contain a version number. There are non-generic and generic versions of `IIntegrationEventHandler`.

```csharp
public interface IIntegrationEventHandler
{
    string Topic { get; set; }

    Task HandleAsync(IIntegrationEvent @event);
}

public interface IIntegrationEventHandler<in TIntegrationEvent> : IIntegrationEventHandler
    where TIntegrationEvent : IIntegrationEvent
{
    Task HandleAsync(TIntegrationEvent @event);
}
```

The generic version of `IntegrationEventHandler` includes a `TIntegrationEvent` type argument that must implement `IIntegrationEvent`. The `IntegrationEvent` abstract record provides defaults for both `Id` and `CreationDate` properties.

```csharp
public interface IIntegrationEvent
{
    string Id { get; }

    DateTime CreationDate { get; }
}
```

```csharp
public abstract record IntegrationEvent : IIntegrationEvent
{
    public string Id { get; init; } = Guid.NewGuid().ToString();

    public DateTime CreationDate { get; init; } = DateTime.UtcNow;
}
```

### EventDriven.EventBus.Dapr

The **EventDriven.EventBus.Dapr** package has a `DaprEventBus` class that extends `EventBus` by injecting `DaprClient`. It also injects `DaprEventBusOptions` for the pubsub component name needed by `DaprClient.PublishAsync`. The event topic defaults to the _type name_ of the the event, but it can also be supplied explicitly.

```csharp
public class DaprEventBus : EventBus
{
    private readonly IOptions<DaprEventBusOptions> _options;
    private readonly DaprClient _dapr;

    public DaprEventBus(IOptions<DaprEventBusOptions> options, DaprClient dapr)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _dapr = dapr ?? throw new ArgumentNullException(nameof(dapr));
    }

    public override async Task PublishAsync<TIntegrationEvent>(
        TIntegrationEvent @event,
        string topic = null,
        string prefix = null)
    {
        if (@event is null) throw new ArgumentNullException(nameof(@event));
        var topicName = GetTopicName(@event.GetType(), topic, prefix);
        await _dapr.PublishEventAsync(_options.Value.PubSubName, topicName, @event);
    }
}
```

The `ServiceCollectionExtensions` class has a `AddDaprEventBus` method that registers `DaprClient` and `DaprEventBus`, and it configures `DaprEventBusOptions` for specifying the `PubSubName` option.

The `DaprEventBusEndpointRouteBuilderExtensions` class has a `MapDaprEventBus` method that allows the caller to subscribe to `DaprEventBus` by adding handlers. It maps an _HTTP Post_ endpoint for each event bus topic that is called by Dapr when a message is sent to the registered pub/sub component. The default component Redis, but Dapr can be configured to use another message broker, such as AWS SNS+SQS.