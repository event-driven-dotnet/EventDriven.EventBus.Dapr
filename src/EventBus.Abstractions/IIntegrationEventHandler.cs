using System.Threading.Tasks;

namespace EventBus.Abstractions
{
    /// <summary>
    /// Handler for integration events.
    /// </summary>
    public interface IIntegrationEventHandler
    {
        /// <summary>
        /// Handler topic.
        /// </summary>
        string Topic { get; set; }

        /// <summary>
        /// Handle an event asynchronously.
        /// </summary>
        /// <param name="event">Integration event.</param>
        /// <returns>Task that will complete when the operation has completed.</returns>
        Task HandleAsync(IIntegrationEvent @event);
    }

    /// <summary>
    /// Handler for integration events.
    /// </summary>
    /// <typeparam name="TIntegrationEvent">Integration event type.</typeparam>
    public interface IIntegrationEventHandler<in TIntegrationEvent> : IIntegrationEventHandler
        where TIntegrationEvent : IIntegrationEvent
    {
        /// <inheritdoc cref="IIntegrationEventHandler" />
        Task HandleAsync(TIntegrationEvent @event);
    }
}
