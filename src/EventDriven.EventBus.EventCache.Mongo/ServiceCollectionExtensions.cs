using EventDriven.DependencyInjection.URF.Mongo;
using EventDriven.EventBus.Abstractions;
using EventDriven.EventBus.EventCache.Mongo;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using URF.Core.Abstractions;
using URF.Core.Mongo;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods for <see cref="T:IServiceCollection" />.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add Mongo event cache to the provided <see cref="T:IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="T:IServiceCollection" /></param>
    /// <param name="configuration">The application's <see cref="IConfiguration"/>.</param>
    /// <param name="lifetime">Service lifetime.</param>
    /// <returns>The original <see cref="T:IServiceCollection" />.</returns>
    public static IServiceCollection AddMongoEventCache(this IServiceCollection services,
        IConfiguration configuration, ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        var eventCacheOptions = new EventCacheOptions
        {
            EnableEventCacheCleanup = true,
        };
        var mongoOptionsConfigSection = configuration.GetSection("MongoEventCacheOptions");
        mongoOptionsConfigSection.Bind(eventCacheOptions);
        if (!mongoOptionsConfigSection.Exists())
            throw new Exception("Configuration section 'MongoEventCacheOptions' not present in app settings.");
        if (eventCacheOptions.EnableEventCache && string.IsNullOrWhiteSpace(eventCacheOptions.AppName))
            throw new Exception("Configuration section 'MongoEventCacheOptions:AppName' must be specified.");

        if (!eventCacheOptions.EnableEventCache) return services;

        services.AddEventCacheImpl(eventCacheOptions, lifetime);
        return services.AddMongoDbSettings<MongoStoreDatabaseSettings, EventWrapperDto>(configuration, lifetime);
    }

    /// <summary>
    /// Add Mongo event cache to the provided <see cref="T:IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="T:IServiceCollection" /></param>
    /// <param name="configureEventCacheOptions">Configure event cache options.</param>
    /// <param name="configureMongoStoreOptions">Configure Mongo store options.</param>
    /// <param name="lifetime">Service lifetime.</param>
    /// <returns>The original <see cref="T:IServiceCollection" />.</returns>
    public static IServiceCollection AddMongoEventCache(this IServiceCollection services,
        Action<EventCacheOptions>? configureEventCacheOptions = null,
        Action<MongoStoreDatabaseSettings>? configureMongoStoreOptions = null,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        var eventCacheOptions = new EventCacheOptions
        {
            EnableEventCache = true,
            EnableEventCacheCleanup = true
        };
        configureEventCacheOptions?.Invoke(eventCacheOptions);
        if (!eventCacheOptions.EnableEventCache) return services;

        services.AddEventCacheImpl(eventCacheOptions, lifetime);
        return services.AddMongoStoreDatabaseSettings(configureMongoStoreOptions, lifetime);
    }

    private static IServiceCollection AddEventCacheImpl(this IServiceCollection services,
        EventCacheOptions eventCacheOptions,
        ServiceLifetime lifetime)
    {
        services.Configure<EventCacheOptions>(options =>
        {
            options.AppName = eventCacheOptions.AppName;
            options.EnableEventCache = eventCacheOptions.EnableEventCache;
            options.EventCacheTimeout = eventCacheOptions.EventCacheTimeout;
            options.EnableEventCacheCleanup = eventCacheOptions.EnableEventCacheCleanup;
            options.EventCacheCleanupInterval = eventCacheOptions.EventCacheCleanupInterval;
        });

        switch (lifetime)
        {
            case ServiceLifetime.Transient:
                services.AddTransient<EventCacheOptions>();
                services.AddTransient<IEventCache, MongoEventCache>();
                services.AddTransient<IEventHandlingRepository<IntegrationEvent>,
                    MongoEventHandlingRepository<IntegrationEvent>>();
                services.AddTransient<IDocumentRepository<EventWrapperDto>, DocumentRepository<EventWrapperDto>>();
                break;
            case ServiceLifetime.Scoped:
                services.AddScoped<EventCacheOptions>();
                services.AddScoped<IEventCache, MongoEventCache>();
                services.AddScoped<IEventHandlingRepository<IntegrationEvent>,
                    MongoEventHandlingRepository<IntegrationEvent>>();
                services.AddScoped<IDocumentRepository<EventWrapperDto>, DocumentRepository<EventWrapperDto>>();
                break;
            default:
                services.AddSingleton(eventCacheOptions);
                services.AddSingleton<IEventCache, MongoEventCache>();
                services.AddSingleton<IEventHandlingRepository<IntegrationEvent>,
                    MongoEventHandlingRepository<IntegrationEvent>>();
                services.AddSingleton<IDocumentRepository<EventWrapperDto>, DocumentRepository<EventWrapperDto>>();
                break;
        }

        return services;
    }

    private static IServiceCollection AddMongoStoreDatabaseSettings(this IServiceCollection services,
        Action<MongoStoreDatabaseSettings>? configureMongoStoreOptions = null,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        var databaseSettings = new MongoStoreDatabaseSettings();
        if (configureMongoStoreOptions != null)
        {
            configureMongoStoreOptions(databaseSettings);
            services.Configure(configureMongoStoreOptions);
        }

        switch (lifetime)
        {
            case ServiceLifetime.Transient:
                return services.AddTransient(_ =>
                {
                    var client = new MongoClient(databaseSettings.ConnectionString);
                    var database = client.GetDatabase(databaseSettings.DatabaseName);
                    return database.GetCollection<EventWrapperDto>(databaseSettings.CollectionName);
                });
            case ServiceLifetime.Scoped:
                return services.AddScoped(_ =>
                {
                    var client = new MongoClient(databaseSettings.ConnectionString);
                    var database = client.GetDatabase(databaseSettings.DatabaseName);
                    return database.GetCollection<EventWrapperDto>(databaseSettings.CollectionName);
                });
            default:
                return services.AddSingleton(_ =>
                {
                    var client = new MongoClient(databaseSettings.ConnectionString);
                    var database = client.GetDatabase(databaseSettings.DatabaseName);
                    return database.GetCollection<EventWrapperDto>(databaseSettings.CollectionName);
                });
        }
    }
}