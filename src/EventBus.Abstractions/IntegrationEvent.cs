using System;

namespace EventBus.Abstractions
{
    /// <inheritdoc cref="IIntegrationEvent" />
    public abstract record IntegrationEvent : IIntegrationEvent
    {
        ///<inheritdoc/>
        public Guid Id { get; init; } = Guid.NewGuid();

        ///<inheritdoc/>
        public DateTime CreationDate { get; init; } = DateTime.UtcNow;
    }
}
