using System;
using EventDriven.EventBus.Abstractions;
using EventDriven.EventBus.Dapr;
using EventDriven.SchemaRegistry.Abstractions;
using EventDriven.SchemaRegistry.Mongo;
using EventDriven.SchemaValidator.Json;
using Microsoft.Extensions.Configuration;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Provides extension methods for <see cref="T:IServiceCollection" />.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds DaprEventBus services to the provided <see cref="T:IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="T:IServiceCollection" /></param>
        /// <param name="configuration">The application's <see cref="IConfiguration"/>.</param>
        /// <param name="useSchemaRegistry">True to use schema registry</param>
        /// <returns>The original <see cref="T:IServiceCollection" />.</returns>
        public static IServiceCollection AddDaprEventBus(this IServiceCollection services,
            IConfiguration configuration, bool useSchemaRegistry = false)
        {
            services.AddDaprClient();
            var daprEventBusOptions = new DaprEventBusOptions();
            var daprOptionsConfigSection = configuration.GetSection(nameof(DaprEventBusOptions));
            daprOptionsConfigSection.Bind(daprEventBusOptions);
            if (!daprOptionsConfigSection.Exists())
                throw new Exception($"Configuration section '{nameof(DaprEventBusOptions)}' not present in app settings.");
            services.Configure<DaprEventBusOptions>(daprOptionsConfigSection);

            Action<DaprEventBusSchemaOptions>? configureSchemaOptions = null;
            if (useSchemaRegistry)
            {
                services.AddSingleton<IEventBus, DaprEventBusWithSchemaRegistry>();
                var eventBusSchemaOptions = new DaprEventBusSchemaOptions();
                var schemaConfigSection = configuration.GetSection(nameof(DaprEventBusSchemaOptions));
                if (!schemaConfigSection.Exists())
                    throw new Exception($"Configuration section '{nameof(DaprEventBusSchemaOptions)}' not present in app settings.");
                
                schemaConfigSection.Bind(eventBusSchemaOptions);
                configureSchemaOptions = options =>
                {
                    options.UseSchemaRegistry = eventBusSchemaOptions.UseSchemaRegistry;
                    options.SchemaRegistryType = eventBusSchemaOptions.SchemaRegistryType;
                    options.MongoStateStoreOptions = eventBusSchemaOptions.MongoStateStoreOptions;
                    options.SchemaValidatorType = eventBusSchemaOptions.SchemaValidatorType;
                    options.AddSchemaOnPublish = eventBusSchemaOptions.AddSchemaOnPublish;
                };
            }
            else
            {
                services.AddSingleton<IEventBus, DaprEventBus>();
            }

            return services.AddDaprEventBusSchema(configureSchemaOptions);
        }

        /// <summary>
        /// Adds DaprEventBus services to the provided <see cref="T:IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="T:IServiceCollection" /></param>
        /// <param name="pubSubName">The name of the pub sub component to use.</param>
        /// <param name="configureSchemaOptions">Configure schema registry options.</param>
        /// <returns>The original <see cref="T:IServiceCollection" />.</returns>
        public static IServiceCollection AddDaprEventBus(this IServiceCollection services, string pubSubName,
            Action<DaprEventBusSchemaOptions>? configureSchemaOptions = null)
        {
            services.AddDaprClient();
            if (configureSchemaOptions != null)
                services.AddSingleton<IEventBus, DaprEventBusWithSchemaRegistry>();
            else
                services.AddSingleton<IEventBus, DaprEventBus>();
            services.Configure<DaprEventBusOptions>(options => options.PubSubName = pubSubName );

            return services.AddDaprEventBusSchema(configureSchemaOptions);
        }

        private static IServiceCollection AddDaprEventBusSchema(this IServiceCollection services,
            Action<DaprEventBusSchemaOptions>? configureSchemaOptions = null)
        {
            if (configureSchemaOptions == null) return services;
            var schemaOptions = new DaprEventBusSchemaOptions
            {
                UseSchemaRegistry = false,
                MongoStateStoreOptions = new MongoStateStoreOptions()
            };
            configureSchemaOptions(schemaOptions);
            services.Configure(configureSchemaOptions);

            if (schemaOptions.SchemaValidatorType == SchemaValidatorType.Json)
            {
                services.AddSingleton<ISchemaGenerator, JsonSchemaGenerator>();
                services.AddSingleton<ISchemaValidator, JsonSchemaValidator>();
            }

            switch (schemaOptions.SchemaRegistryType)
            {
                case SchemaRegistryType.Mongo:
                    services.AddMongoSchemaRegistry(options =>
                    {
                        options.ConnectionString = schemaOptions.MongoStateStoreOptions.ConnectionString;
                        options.DatabaseName = schemaOptions.MongoStateStoreOptions.DatabaseName;
                        options.CollectionName = schemaOptions.MongoStateStoreOptions.CollectionName;
                    });
                    break;
            }
            return services;
        }
    }
}
