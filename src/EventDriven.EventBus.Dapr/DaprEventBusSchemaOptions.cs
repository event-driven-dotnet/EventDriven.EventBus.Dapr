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
        /// The name of the state store component to use.
        /// </summary>
        public string SchemaRegistryStateStoreName { get; set; } = "statestore";

        /// <summary>
        /// Schema validator type.
        /// </summary>
        public SchemaValidatorType SchemaValidatorType { get; set; }

        /// <summary>
        /// Add schema to registry on publish if not previously registered.
        /// </summary>
        public bool AddSchemaOnPublish { get; set; }
    }
}
