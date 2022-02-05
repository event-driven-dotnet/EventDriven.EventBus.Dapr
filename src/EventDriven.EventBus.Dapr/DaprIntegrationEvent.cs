using EventDriven.EventBus.Abstractions;

namespace EventDriven.EventBus.Dapr;

/// <summary>
/// Dapr integration event.
/// </summary>
public record DaprIntegrationEvent : IntegrationEvent;