using Dapr.Client;
using Microsoft.Extensions.Options;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using EventDriven.SchemaRegistry.Abstractions;
using Microsoft.Extensions.Logging;

namespace EventDriven.EventBus.Dapr
{
    /// <summary>
    /// Uses Dapr to provide a way for systems to communicate without knowing about each other.
    /// </summary>
    public class DaprEventBusWithSchemaRegistry : DaprEventBus
    {
        private readonly IOptions<DaprEventBusSchemaOptions> _daprEventBusSchemaOptions;
        private readonly ILogger<DaprEventBus> _logger;
        private readonly ISchemaGenerator _schemaGenerator;
        private readonly ISchemaValidator _schemaValidator;
        private readonly ISchemaRegistry _schemaRegistry;

        /// <summary>
        /// DaprEventBus constructor.
        /// </summary>
        /// <param name="dapr">Client for interacting with Dapr endpoints.</param>
        /// <param name="schemaGenerator">Schema generator.</param>
        /// <param name="schemaValidator">Schema validator.</param>
        /// <param name="schemaRegistry">Schema registry.</param>
        /// <param name="daprEventBusOptions">DaprEventBus options.</param>
        /// <param name="daprEventBusSchemaOptions">Schema registry options.</param>
        /// <param name="logger">Logger for DaprEventBus.</param>
        public DaprEventBusWithSchemaRegistry(
            DaprClient dapr,
            ISchemaGenerator schemaGenerator,
            ISchemaValidator schemaValidator,
            ISchemaRegistry schemaRegistry,
            IOptions<DaprEventBusOptions> daprEventBusOptions,
            IOptions<DaprEventBusSchemaOptions> daprEventBusSchemaOptions,
            ILogger<DaprEventBusWithSchemaRegistry> logger) : base(dapr, daprEventBusOptions)
        {
            _schemaGenerator = schemaGenerator ?? throw new ArgumentNullException(nameof(schemaGenerator));
            _schemaValidator = schemaValidator ?? throw new ArgumentNullException(nameof(schemaValidator));
            _schemaRegistry = schemaRegistry ?? throw new ArgumentNullException(nameof(schemaRegistry));
            _daprEventBusSchemaOptions = daprEventBusSchemaOptions ?? throw new ArgumentNullException(nameof(daprEventBusSchemaOptions));
            _logger = logger;
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

            if (_daprEventBusSchemaOptions.Value.UseSchemaRegistry)
            {
                // Get schema
                var schema = await _schemaRegistry.GetSchema(topicName);
                if (schema == null)
                {
                    if (!_daprEventBusSchemaOptions.Value.AddSchemaOnPublish)
                    {
                        _logger.LogError("No schema registered for {TopicName}", topicName);
                        throw new SchemaNotRegisteredException(topicName);
                    }
                    
                    // Generate schema
                    var content = _schemaGenerator.GenerateSchema(typeof(TIntegrationEvent));
                    if (string.IsNullOrWhiteSpace(content))
                    {
                        _logger.LogError("Schema generation failed for {TopicName}", topicName);
                        throw new Exception($"Schema generation failed for {topicName}");
                    }
                    
                    // Register schema
                    schema = new Schema
                    {
                        Topic = topicName,
                        Content = content
                    };
                    await _schemaRegistry.AddSchema(schema);
                    _logger.LogInformation("Schema registered for {TopicName}", topicName);
                }
                
                // Validate message with schema
                var message = JsonSerializer.Serialize(@event, typeof(TIntegrationEvent), DaprClient.JsonSerializerOptions);
                var isValid = _schemaValidator.ValidateMessage(message, schema.Content, out var errorMessages);
                if (!isValid)
                {
                    _logger.LogError("Schema validation failed for {TopicName}", topicName);
                    foreach (var errorMessage in errorMessages)
                        _logger.LogError("Schema validation error: {ErrorMessage}", errorMessage);
                    throw new SchemaValidationException(topicName);
                }
            }

            await DaprClient.PublishEventAsync(DaprEventBusOptions.Value.PubSubName, topicName, @event);
        }
    }
}
