using EventDriven.SchemaRegistry.Mongo;

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
        /// Schema registry type.
        /// </summary>
        public SchemaRegistryType SchemaRegistryType { get; set; }

        /// <summary>
        /// Schema validator type.
        /// </summary>
        public SchemaValidatorType SchemaValidatorType { get; set; }

        /// <summary>
        /// Mongo state store options.
        /// </summary>
        public MongoStateStoreOptions MongoStateStoreOptions { get; set; } = null!;

        /// <summary>
        /// Add schema to registry on publish if not previously registered.
        /// </summary>
        public bool AddSchemaOnPublish { get; set; }
    }
}
