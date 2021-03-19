using System;

namespace EventBus.Abstractions
{
    ///<inheritdoc/>
    public abstract record IntegrationEvent : IIntegrationEvent
    {
        ///<inheritdoc/>
        public Guid Id { get; init; } = Guid.NewGuid();

        ///<inheritdoc/>
        public DateTime CreationDate { get; init; } = DateTime.UtcNow;
    }
}
