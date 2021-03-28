using Dapr.Client;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace EventBus.Dapr
{
    /// <summary>
    /// Uses Dapr to provide a way for systems to communicate without knowing about each other.
    /// </summary>
    public class DaprEventBus : Abstractions.EventBus
    {
        private readonly IOptions<DaprEventBusOptions> _options;
        private readonly DaprClient _dapr;

        /// <summary>
        /// DaprEventBus constructor.
        /// </summary>
        /// <param name="options">DaprEventBus options.</param>
        /// <param name="dapr">Client for interacting with Dapr endpoints.</param>
        public DaprEventBus(IOptions<DaprEventBusOptions> options, DaprClient dapr)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _dapr = dapr ?? throw new ArgumentNullException(nameof(dapr));
        }

        ///<inheritdoc/>
        public override async Task PublishAsync<TIntegrationEvent>(TIntegrationEvent @event, string topic = null)
        {
            if (@event is null) throw new ArgumentNullException(nameof(@event));
            topic ??= @event.GetType().Name;
            await _dapr.PublishEventAsync(_options.Value.PubSubName, topic, @event);
        }
    }
}
