using System.Threading.Tasks;

namespace EventBus.Abstractions
{
    ///<inheritdoc/>
    public abstract class IntegrationEventHandler<TIntegrationEvent> : IIntegrationEventHandler<TIntegrationEvent>
        where TIntegrationEvent : IIntegrationEvent
    {
        /// <summary>
        /// IntegrationEventHandler constructor.
        /// </summary>
        protected IntegrationEventHandler()
        {
            Topic = typeof(TIntegrationEvent).Name;
        }

        /// <summary>
        /// IntegrationEventHandler constructor.
        /// </summary>
        /// <param name="topic">Event handler topic.</param>
        protected IntegrationEventHandler(string topic)
        {
            Topic = topic;
        }

        /// <summary>
        /// Event handler topic.
        /// </summary>
        public string Topic { get; set; }

        ///<inheritdoc/>
        public abstract Task HandleAsync(TIntegrationEvent @event);

        ///<inheritdoc/>
        public virtual Task HandleAsync(IIntegrationEvent @event) => HandleAsync((TIntegrationEvent)@event);
    }
}
