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
        [Obsolete("This version of AddDaprEventBus is obsolete. Instead use the version that omits the useSchemaRegistry parameter.", true)]
        public static IServiceCollection AddDaprEventBus(this IServiceCollection services,
            IConfiguration configuration, bool useSchemaRegistry) =>
            AddDaprEventBus(services, configuration);

        /// <summary>
        /// Adds DaprEventBus services to the provided <see cref="T:IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="T:IServiceCollection" /></param>
        /// <param name="configuration">The application's <see cref="IConfiguration"/>.</param>
        /// <param name="lifetime">Service lifetime.</param>
        /// <returns>The original <see cref="T:IServiceCollection" />.</returns>
        public static IServiceCollection AddDaprEventBus(
            this IServiceCollection services, IConfiguration configuration,
            ServiceLifetime lifetime = ServiceLifetime.Singleton)
        {
            var daprEventBusOptions = new DaprEventBusOptions();
            var daprOptionsConfigSection = configuration.GetSection(nameof(DaprEventBusOptions));
            daprOptionsConfigSection.Bind(daprEventBusOptions);
            if (!daprOptionsConfigSection.Exists())
                throw new Exception($"Configuration section '{nameof(DaprEventBusOptions)}' not present in app settings.");
            services.Configure<DaprEventBusOptions>(daprOptionsConfigSection);

            Action<DaprEventBusSchemaOptions>? configureSchemaOptions = null;
            var eventBusSchemaOptions = new DaprEventBusSchemaOptions();
            var schemaConfigSection = configuration.GetSection(nameof(DaprEventBusSchemaOptions));
            schemaConfigSection.Bind(eventBusSchemaOptions);
            if (schemaConfigSection.Exists())
            {
                configureSchemaOptions = options =>
                {
                    options.UseSchemaRegistry = eventBusSchemaOptions.UseSchemaRegistry;
                    options.SchemaRegistryType = eventBusSchemaOptions.SchemaRegistryType;
                    options.MongoStateStoreOptions = eventBusSchemaOptions.MongoStateStoreOptions;
                    options.SchemaValidatorType = eventBusSchemaOptions.SchemaValidatorType;
                    options.AddSchemaOnPublish = eventBusSchemaOptions.AddSchemaOnPublish;
                };
            }
            return services.AddDaprEventBus(daprEventBusOptions.PubSubName, configureSchemaOptions, lifetime);
        }

        /// <summary>
        /// Adds DaprEventBus services to the provided <see cref="T:IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="T:IServiceCollection" /></param>
        /// <param name="pubSubName">The name of the pub sub component to use.</param>
        /// <param name="configureSchemaOptions">Configure schema registry options.</param>
        /// <param name="lifetime">Service lifetime.</param>
        /// <returns>The original <see cref="T:IServiceCollection" />.</returns>
        public static IServiceCollection AddDaprEventBus(this IServiceCollection services, string pubSubName,
            Action<DaprEventBusSchemaOptions>? configureSchemaOptions = null,
            ServiceLifetime lifetime = ServiceLifetime.Singleton)
        {
            services.AddDaprClient();
            if (configureSchemaOptions == null)
            {
                switch (lifetime)
                {
                    case ServiceLifetime.Transient:
                        services.AddTransient<IEventBus, DaprEventBus>();
                        break;
                    case ServiceLifetime.Scoped:
                        services.AddScoped<IEventBus, DaprEventBus>();
                        break;
                    default:
                        services.AddSingleton<IEventBus, DaprEventBus>();
                        break;
                }
            }
            else
            {
                var schemaOptions = new DaprEventBusSchemaOptions
                {
                    UseSchemaRegistry = false,
                    MongoStateStoreOptions = new MongoStateStoreOptions()
                };
                configureSchemaOptions(schemaOptions);
                if (!schemaOptions.UseSchemaRegistry)
                {
                    switch (lifetime)
                    {
                        case ServiceLifetime.Transient:
                            services.AddTransient<IEventBus, DaprEventBus>();
                            break;
                        case ServiceLifetime.Scoped:
                            services.AddScoped<IEventBus, DaprEventBus>();
                            break;
                        default:
                            services.AddSingleton<IEventBus, DaprEventBus>();
                            break;
                    }
                }
                else
                {
                    switch (lifetime)
                    {
                        case ServiceLifetime.Transient:
                            services.AddTransient<IEventBus, DaprEventBusWithSchemaRegistry>();
                            break;
                        case ServiceLifetime.Scoped:
                            services.AddScoped<IEventBus, DaprEventBusWithSchemaRegistry>();
                            break;
                        default:
                            services.AddSingleton<IEventBus, DaprEventBusWithSchemaRegistry>();
                            break;
                    }
                    services.AddDaprEventBusSchema(configureSchemaOptions, lifetime);
                }
            }

            return services.Configure<DaprEventBusOptions>(options => options.PubSubName = pubSubName);
        }

        private static IServiceCollection AddDaprEventBusSchema(this IServiceCollection services,
            Action<DaprEventBusSchemaOptions>? configureSchemaOptions = null,
            ServiceLifetime lifetime = ServiceLifetime.Singleton)
        {
            if (configureSchemaOptions == null) return services;
            var schemaOptions = new DaprEventBusSchemaOptions
            {
                UseSchemaRegistry = false,
                MongoStateStoreOptions = new MongoStateStoreOptions()
            };
            configureSchemaOptions(schemaOptions);
            services.Configure(configureSchemaOptions);

            if (!schemaOptions.UseSchemaRegistry) return services;
            if (schemaOptions.SchemaValidatorType == SchemaValidatorType.Json)
            {
                switch (lifetime)
                {
                    case ServiceLifetime.Transient:
                        services.AddTransient<ISchemaGenerator, JsonSchemaGenerator>();
                        services.AddTransient<ISchemaValidator, JsonSchemaValidator>();
                        break;
                    case ServiceLifetime.Scoped:
                        services.AddScoped<ISchemaGenerator, JsonSchemaGenerator>();
                        services.AddScoped<ISchemaValidator, JsonSchemaValidator>();
                        break;
                    default:
                        services.AddSingleton<ISchemaGenerator, JsonSchemaGenerator>();
                        services.AddSingleton<ISchemaValidator, JsonSchemaValidator>();
                        break;
                }
            }

            switch (schemaOptions.SchemaRegistryType)
            {
                case SchemaRegistryType.Mongo:
                    services.AddMongoSchemaRegistry(options =>
                    {
                        options.ConnectionString = schemaOptions.MongoStateStoreOptions.ConnectionString;
                        options.DatabaseName = schemaOptions.MongoStateStoreOptions.DatabaseName;
                        options.CollectionName = schemaOptions.MongoStateStoreOptions.CollectionName;
                    }, lifetime);
                    break;
            }
            return services;
        }
    }
}
