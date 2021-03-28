using System;

namespace EventBus.Abstractions
{
    /// <summary>
    /// Event for communicating information between systems.
    /// </summary>
    public interface IIntegrationEvent
    {
        /// <summary>
        /// Unique event identifier.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Event creation date.
        /// </summary>
        DateTime CreationDate { get; }
    }
}
