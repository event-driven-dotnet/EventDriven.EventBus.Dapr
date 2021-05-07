using System;

namespace EventDriven.EventBus.Dapr
{
    /// <summary>
    /// Schema validation exception.
    /// </summary>
    public class SchemaValidationException : Exception
    {
        /// <summary>
        /// Schema validation failed for specified topic
        /// </summary>
        /// <param name="topicName">Fully qualified topic name.</param>
        public SchemaValidationException(string topicName) : base($"Schema validation failed for the topic '{topicName}'")
        {
        }
    }
}