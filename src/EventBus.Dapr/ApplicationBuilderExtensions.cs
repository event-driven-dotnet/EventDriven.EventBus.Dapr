using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EventBus.Abstractions;
using EventBus.Dapr;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Provides extension methods for <see cref="T:Microsoft.AspNetCore.Builder" />.
    /// </summary>
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// Adds DaprEventBus to the middleware pipeline.
        /// </summary>
        /// <param name="app">An <see cref="T:Microsoft.AspNetCore.Builder.IApplicationBuilder" />.</param>
        /// <param name="configure">The original <see cref="T:Microsoft.AspNetCore.Builder.IApplicationBuilder" />.</param>
        /// <returns></returns>
        public static IApplicationBuilder UseDaprEventBus(this IApplicationBuilder app, 
            Action<IEventBus> configure = null)
        {
            app.UseRouting();

            // Get services
            var logger = app.ApplicationServices.GetRequiredService<ILogger<DaprEventBus>>();
            var eventBus = app.ApplicationServices.GetRequiredService<IEventBus>();
            var serializerOptions = app.ApplicationServices.GetRequiredService<JsonSerializerOptions>();
            var eventBusOptions = app.ApplicationServices.GetRequiredService<IOptions<DaprEventBusOptions>>();

            // Configure event bus
            configure?.Invoke(eventBus);

            // Map endpoints
            app.UseCloudEvents();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapSubscribeHandler();
                foreach (var topic in eventBus.Topics)
                {
                    logger.LogInformation($"Mapping Post for topic: {topic.Key}");
                    endpoints.MapPost(topic.Key, HandleMessage)
                        .WithTopic(eventBusOptions.Value.PubSubName, topic.Key);
                }
            });

            async Task HandleMessage(HttpContext context)
            {
                var handlers = GetHandlersForRequest(context.Request.Path);
                logger.LogInformation($"Request handlers count: {handlers.Count}");

                if (handlers != null)
                {
                    foreach (var handler in handlers)
                    {
                        var @event = await GetEventFromRequestAsync(context, handler, serializerOptions);
                        logger.LogInformation($"Handling event: {@event.Id}");
                        await handler.HandleAsync(@event);
                    }
                }
            }

            List<IIntegrationEventHandler> GetHandlersForRequest(string path)
            {
                var topic = path.Substring(path.IndexOf("/") + 1);
                logger.LogInformation($"Topic for request: {topic}");

                if (eventBus.Topics.TryGetValue(topic, out List<IIntegrationEventHandler> handlers))
                    return handlers;
                return null;
            }

            async Task<IIntegrationEvent> GetEventFromRequestAsync(HttpContext context, 
                IIntegrationEventHandler handler, JsonSerializerOptions serializerOptions)
            {
                var eventType = handler.GetType().BaseType.GenericTypeArguments[0];
                var value = await JsonSerializer.DeserializeAsync(context.Request.Body, eventType, serializerOptions);
                return (IIntegrationEvent)value;
            }

            return app;
        }
    }
}
