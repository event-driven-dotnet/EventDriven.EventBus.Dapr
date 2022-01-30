using EventDriven.EventBus.Abstractions;

namespace EventDriven.EventBus.Dapr
{
    /// <summary>
    /// DaprEventBus options.
    /// </summary>
    public class DaprEventBusOptions
    {
        /// <summary>
        /// Dapr PubSub component name.
        /// </summary>
        public string PubSubName { get; set; } = "pubsub";

        /// <summary>
        /// Event bus options.
        /// </summary>
        public EventBusOptions EventBusOptions { get; set; }
    }
}
