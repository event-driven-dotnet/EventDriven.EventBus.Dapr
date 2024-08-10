using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Text.Json;
using System.Threading.Tasks;
using Dapr.Client;
using EventDriven.EventBus.Abstractions;
using EventDriven.EventBus.Dapr;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
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
        /// <param name="configure">Used to subscribe to events with event handlers.</param>
        /// <returns>An <see cref="DaprEventBusEndpointConventionBuilder"/> for endpoints associated with DaprEventBus subscriptions.</returns>
        public static DaprEventBusEndpointConventionBuilder MapDaprEventBus(this IEndpointRouteBuilder endpoints,
            Action<IEventBus?>? configure = null)
        {
            if (endpoints is null)
                throw new ArgumentNullException(nameof(endpoints));

            var logger = endpoints.ServiceProvider.GetService<ILogger<DaprEventBus>>();
            var eventBus = endpoints.ServiceProvider.GetService<IEventBus>();
            var daprClient = endpoints.ServiceProvider.GetService<DaprClient>();
            var daprEventCache = endpoints.ServiceProvider.GetService<IEventCache>();
            var daprEventBusOptions = endpoints.ServiceProvider.GetService<IOptions<DaprEventBusOptions>>();

            // Configure event bus
            logger?.LogInformation("Configuring event bus ...");
            configure?.Invoke(eventBus);

            IEndpointConventionBuilder? builder = null;
            if (eventBus?.Topics != null)
            {
                foreach (var topic in eventBus.Topics)
                {
                    logger?.LogInformation("Mapping Post for topic: {TopicKey}", topic.Key);
                    builder = endpoints.MapPost(topic.Key, HandleMessage)
                        .WithTopic(daprEventBusOptions?.Value.PubSubName, topic.Key);
                }

                async Task HandleMessage(HttpContext context)
                {
                    // Get handlers
                    var handlers = GetHandlersForRequest(context.Request.Path);
                    logger?.LogInformation("Request handlers count: {HandlersCount}", handlers!.Count);
                    var handler1 = handlers!.FirstOrDefault();
                    if (handler1 == null) return;

                    // Get event type
                    var eventType = GetEventType(handler1);

                    // Get event
                    var @event = await GetEventFromRequestAsync(context, eventType, daprClient?.JsonSerializerOptions);

                    // Process handlers
                    var errorOccurred = false;
                    foreach (var handler in handlers!)
                    {
                        logger?.LogInformation("Handling event: {EventId}", @event?.Id);

                        // See if event has been handled by this handler; if not, add event as started
                        string? errorMessage = null;
                        var hasBeenHandled = false;
                        if (daprEventCache != null)
                            hasBeenHandled = await daprEventCache.HasBeenHandledPersistEventAsync(@event!, handler.GetType().Name);
                        
                        try
                        {
                            // Handle the event
                            if (!hasBeenHandled)
                                await handler.HandleAsync(@event!);
                        }
                        catch (Exception e)
                        {
                            logger?.LogInformation("Handler threw exception: {Message}", e);
                            errorOccurred = true;
                            errorMessage = e.Message;
                        }

                        // Add event to cache
                        if (!hasBeenHandled && daprEventCache != null)
                            await daprEventCache.UpdateEventAsync(@event!, handler.GetType().Name, errorMessage);
                    }
                    
                    // If any handler has thrown an exception, return 500 so Dapr can retry sending the message.
                    if (errorOccurred) SetErrorStatus(context);
                }
            }

            void SetErrorStatus(HttpContext context)
            {
                // Set status code to 500 if retries have not been disabled
                var statusCode = StatusCodes.Status500InternalServerError;

                // Set status code to 400 if retries have been disabled
                if (daprEventBusOptions is { Value.DisableRetries: true })
                    statusCode = StatusCodes.Status404NotFound;
                context.Response.StatusCode = statusCode;
            }

            List<IIntegrationEventHandler>? GetHandlersForRequest(string path)
            {
                var topic = path.Substring(path.IndexOf("/", StringComparison.Ordinal) + 1);
                logger?.LogInformation("Topic for request: {Topic}", topic);
                return eventBus.Topics.TryGetValue(topic, out var handlers) ? handlers : null;
            }

            Type? GetEventType(IIntegrationEventHandler handler)
            {
                var eventType = handler.GetType().BaseType?.GenericTypeArguments[0];
                if (eventType != null) return eventType;
                logger?.LogInformation("Cannot determine event type");
                return null;
            }

            async Task<IntegrationEvent?> GetEventFromRequestAsync(HttpContext context, 
                Type? eventType, JsonSerializerOptions? serializerOptions)
            {
                // Check content type
                if (!string.Equals(context.Request.ContentType, MediaTypeNames.Application.Json,
                    StringComparison.Ordinal))
                {
                    logger?.LogInformation("Unsupported Content-Type header: {ContentType}",
                        context.Request.ContentType);
                    return null;
                }
                
                try
                {
                    // Get event
                    var value = await JsonSerializer.DeserializeAsync(context.Request.Body, eventType!, serializerOptions);
                    return (IntegrationEvent)value!;
                }
                catch (Exception e) when (e is JsonException || e is ArgumentNullException || e is NotSupportedException)
                {
                    logger?.LogInformation("Unable to deserialize event from request '{RequestPath}': {Message}",
                        context.Request.Path, e.Message);
                    return null;
                }
            }

            return new DaprEventBusEndpointConventionBuilder(builder!);
        }
    }
}