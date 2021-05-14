namespace EventDriven.EventBus.Dapr
{
    /// <summary>
    /// Schema registry type.
    /// </summary>
    public enum SchemaRegistryType
    {
        /// <summary>
        /// Dapr schema registry.
        /// </summary>
        Dapr,

        /// <summary>
        /// Mongo schema registry.
        /// </summary>
        Mongo
    }
}