namespace EventDriven.EventBus.Dapr
{
    /// <summary>
    /// DaprEventBus schema options.
    /// </summary>
    public class DaprEventBusSchemaOptions
    {
        /// <summary>
        /// Use schema registry.
        /// </summary>
        public bool UseSchemaRegistry { get; set; }

        /// <summary>
        /// Add schema to registry on publish if not previously registered.
        /// </summary>
        public bool AddSchemaOnPublish { get; set; }
    }
}
