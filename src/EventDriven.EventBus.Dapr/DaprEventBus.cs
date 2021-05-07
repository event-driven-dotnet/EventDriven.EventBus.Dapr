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
    public class DaprEventBus : Abstractions.EventBus
    {
        private readonly IOptions<DaprEventBusOptions> _options;
        private readonly IOptions<DaprEventBusSchemaOptions> _schemaOptions;
        private readonly ILogger<DaprEventBus> _logger;
        private readonly DaprClient _dapr;
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
        /// <param name="options">DaprEventBus options.</param>
        /// <param name="schemaOptions">Schema registry options.</param>
        /// <param name="logger">Logger for DaprEventBus.</param>
        public DaprEventBus(
            DaprClient dapr,
            ISchemaGenerator schemaGenerator,
            ISchemaValidator schemaValidator,
            ISchemaRegistry schemaRegistry,
            IOptions<DaprEventBusOptions> options,
            IOptions<DaprEventBusSchemaOptions> schemaOptions,
            ILogger<DaprEventBus> logger)
        {
            _dapr = dapr ?? throw new ArgumentNullException(nameof(dapr));
            _schemaGenerator = schemaGenerator ?? throw new ArgumentNullException(nameof(schemaGenerator));
            _schemaValidator = schemaValidator ?? throw new ArgumentNullException(nameof(schemaValidator));
            _schemaRegistry = schemaRegistry ?? throw new ArgumentNullException(nameof(schemaRegistry));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _schemaOptions = schemaOptions ?? throw new ArgumentNullException(nameof(schemaOptions));
            _logger = logger;
        }

        ///<inheritdoc/>
        public override async Task PublishAsync<TIntegrationEvent>(
            TIntegrationEvent @event,
            string topic = null,
            string prefix = null)
        {
            if (@event is null) throw new ArgumentNullException(nameof(@event));
            var topicName = GetTopicName(@event.GetType(), topic, prefix);

            if (_schemaOptions.Value.UseSchemaRegistry)
            {
                // Get schema
                var schema = await _schemaRegistry.GetSchema(topicName);
                if (string.IsNullOrWhiteSpace(schema))
                {
                    if (!_schemaOptions.Value.AddSchemaOnPublish)
                    {
                        _logger.LogError("No schema registered for {TopicName}", topicName);
                        throw new SchemaNotRegisteredException(topicName);
                    }
                    
                    // Generate schema
                    schema = _schemaGenerator.GenerateSchema(typeof(TIntegrationEvent));
                    if (string.IsNullOrWhiteSpace(schema))
                    {
                        _logger.LogError("Schema generation failed for {TopicName}", topicName);
                        throw new Exception($"Schema generation failed for {topicName}");
                    }
                    
                    // Register schema
                    await _schemaRegistry.AddSchema(topicName, schema);
                    _logger.LogInformation("Schema registered for {TopicName}", topicName);
                }
                
                // Validate message with schema
                var message = JsonSerializer.Serialize(@event, typeof(TIntegrationEvent), _dapr.JsonSerializerOptions);
                var isValid = _schemaValidator.ValidateMessage(message, schema, out var errorMessages);
                if (!isValid)
                {
                    _logger.LogError("Schema validation failed for {TopicName}", topicName);
                    foreach (var errorMessage in errorMessages)
                        _logger.LogError("Schema validation error: {ErrorMessage}", errorMessage);
                    throw new SchemaValidationException(topicName);
                }
            }

            await _dapr.PublishEventAsync(_options.Value.PubSubName, topicName, @event);
        }
    }
}
