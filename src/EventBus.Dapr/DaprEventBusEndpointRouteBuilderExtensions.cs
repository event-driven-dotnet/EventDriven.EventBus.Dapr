using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Dapr.Client;
using EventBus.Abstractions;
using EventBus.Dapr;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Provides extension methods for <see cref="IEndpointRouteBuilder" />.
    /// </summary>
    public static class DaprEventBusEndpointRouteBuilderExtensions
    {
        /// <summary>
        /// Maps endpoints that will handle requests for DaprEventBus subscriptions.
        /// </summary>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder" />.</param>
        /// <param name="configure">The original <see cref="T:Microsoft.AspNetCore.Builder.IApplicationBuilder" />.</param>
        /// <returns>An <see cref="DaprEventBusEndpointConventionBuilder"/> for endpoints associated with DaprEventBus subscriptions.</returns>
        public static DaprEventBusEndpointConventionBuilder MapDaprEventBus(this IEndpointRouteBuilder endpoints,
            Action<IEventBus> configure = null)
        {
            if (endpoints is null)
                throw new ArgumentNullException(nameof(endpoints));

            var logger = endpoints.ServiceProvider.GetService<ILogger<DaprEventBus>>();
            var eventBus = endpoints.ServiceProvider.GetService<IEventBus>();
            var daprClient = endpoints.ServiceProvider.GetService<DaprClient>();
            var eventBusOptions = endpoints.ServiceProvider.GetService<IOptions<DaprEventBusOptions>>();

            // Configure event bus
            logger.LogInformation($"Configuring event bus ...");
            configure?.Invoke(eventBus);

            IEndpointConventionBuilder builder = null;
            if (eventBus?.Topics != null)
            {
                foreach (var topic in eventBus.Topics)
                {
                    logger.LogInformation($"Mapping Post for topic: {topic.Key}");
                    builder = endpoints.MapPost(topic.Key, HandleMessage)
                        .WithTopic(eventBusOptions?.Value.PubSubName, topic.Key);
                }

                async Task HandleMessage(HttpContext context)
                {
                    var handlers = GetHandlersForRequest(context.Request.Path);
                    logger.LogInformation($"Request handlers count: {handlers.Count}");

                    foreach (var handler in handlers)
                    {
                        var @event =
                            await GetEventFromRequestAsync(context, handler, daprClient?.JsonSerializerOptions);
                        logger.LogInformation($"Handling event: {@event.Id}");
                        await handler.HandleAsync(@event);
                    }
                }

                List<IIntegrationEventHandler> GetHandlersForRequest(string path)
                {
                    var topic = path.Substring(path.IndexOf("/", StringComparison.Ordinal) + 1);
                    logger.LogInformation($"Topic for request: {topic}");

                    if (eventBus.Topics.TryGetValue(topic, out List<IIntegrationEventHandler> handlers))
                        return handlers;
                    return null;
                }
            }

            async Task<IIntegrationEvent> GetEventFromRequestAsync(HttpContext context, 
                IIntegrationEventHandler handler, JsonSerializerOptions serializerOptions)
            {
                var eventType = handler.GetType().BaseType?.GenericTypeArguments[0];
                var value = await JsonSerializer.DeserializeAsync(context.Request.Body, eventType, serializerOptions);
                return (IIntegrationEvent)value;
            }

            return new DaprEventBusEndpointConventionBuilder(builder);
        }
    }
}