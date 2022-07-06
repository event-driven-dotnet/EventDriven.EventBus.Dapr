using EventDriven.EventBus.Abstractions;

namespace EventDriven.EventBus.Dapr.EventCache.Mongo;

/// <summary>
/// Dapr integration event.
/// </summary>
public record DaprIntegrationEvent : IntegrationEvent;