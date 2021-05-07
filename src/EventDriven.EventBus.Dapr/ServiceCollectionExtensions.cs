using System;
using System.Text.Json;
using EventDriven.EventBus.Abstractions;
using EventDriven.EventBus.Dapr;
using EventDriven.SchemaRegistry.Abstractions;
using EventDriven.SchemaValidator.Json;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Provides extension methods for <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds DaprEventBus services to the provided <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /></param>
        /// <param name="pubSubName">The name of the pub sub component to use.</param>
        /// <param name="configureSchemaOptions">Configure schema registry options.</param>
        /// <returns>The original <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.</returns>
        public static IServiceCollection AddDaprEventBus(this IServiceCollection services, string pubSubName,
            Action<DaprEventBusSchemaOptions> configureSchemaOptions = null)
        {
            services.AddDaprClient(builder =>
                builder.UseJsonSerializationOptions(new JsonSerializerOptions()
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true,
                }));
            services.AddSingleton<IEventBus, DaprEventBus>();
            services.Configure<DaprEventBusOptions>(options => options.PubSubName = pubSubName);

            var schemaOptions = new DaprEventBusSchemaOptions();
            configureSchemaOptions ??= options =>
            {
                options.UseSchemaRegistry = false;
            };
            configureSchemaOptions.Invoke(schemaOptions);
            services.Configure(configureSchemaOptions);

            if (schemaOptions.SchemaValidatorType == SchemaValidatorType.Json)
            {
                services.AddSingleton<ISchemaGenerator, JsonSchemaGenerator>();
                services.AddSingleton<ISchemaValidator, JsonSchemaValidator>();
            }
            services.AddDaprSchemaRegistry(schemaOptions.SchemaRegistryStateStoreName);
            return services;
        }
    }
}
