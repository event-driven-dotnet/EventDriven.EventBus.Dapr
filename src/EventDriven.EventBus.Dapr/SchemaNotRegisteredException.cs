using System;

namespace EventDriven.EventBus.Dapr
{
    /// <summary>
    /// Schema not registered exception.
    /// </summary>
    public class SchemaNotRegisteredException : Exception
    {
        /// <summary>
        /// Schema is not registered for specified topic
        /// </summary>
        /// <param name="topicName">Fully qualified topic name.</param>
        public SchemaNotRegisteredException(string topicName) : base($"No schema is registered for the topic '{topicName}'")
        {
        }
    }
}