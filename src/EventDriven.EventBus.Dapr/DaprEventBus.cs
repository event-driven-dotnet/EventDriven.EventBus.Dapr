using Dapr.Client;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace EventDriven.EventBus.Dapr
{
    /// <summary>
    /// Uses Dapr to provide a way for systems to communicate without knowing about each other.
    /// </summary>
    public class DaprEventBus : Abstractions.EventBus
    {
        /// <summary>
        /// Dapr client.
        /// </summary>
        protected DaprClient DaprClient { get; }
        /// <summary>
        /// Dapr event bus options.
        /// </summary>
        protected IOptions<DaprEventBusOptions> DaprEventBusOptions { get; }

        /// <summary>
        /// DaprEventBus constructor.
        /// </summary>
        /// <param name="dapr">Client for interacting with Dapr endpoints.</param>
        /// <param name="daprEventBusOptions">DaprEventBus options.</param>
        public DaprEventBus(
            DaprClient dapr,
            IOptions<DaprEventBusOptions> daprEventBusOptions)
        {
            DaprClient = dapr ?? throw new ArgumentNullException(nameof(dapr));
            DaprEventBusOptions = daprEventBusOptions ?? throw new ArgumentNullException(nameof(daprEventBusOptions));
        }

        ///<inheritdoc/>
        public override async Task PublishAsync<TIntegrationEvent>(
            TIntegrationEvent @event,
            string? topic = null,
            string? prefix = null, 
            string? suffix = null)
        {
            if (@event is null) throw new ArgumentNullException(nameof(@event));
            var topicName = GetTopicName(@event.GetType(), topic, prefix, suffix);
            await DaprClient.PublishEventAsync(DaprEventBusOptions.Value.PubSubName, topicName, @event);
        }
    }
}
