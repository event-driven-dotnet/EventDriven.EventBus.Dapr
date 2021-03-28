using System;

namespace EventBus.Abstractions
{
    /// <inheritdoc cref="IIntegrationEvent" />
    public abstract record IntegrationEvent : IIntegrationEvent
    {
        ///<inheritdoc/>
        public string Id { get; init; } = Guid.NewGuid().ToString();

        ///<inheritdoc/>
        public DateTime CreationDate { get; init; } = DateTime.UtcNow;
    }
}
