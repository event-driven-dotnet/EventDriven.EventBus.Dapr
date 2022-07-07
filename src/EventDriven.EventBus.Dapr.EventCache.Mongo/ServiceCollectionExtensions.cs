using EventDriven.DependencyInjection.URF.Mongo;
using EventDriven.EventBus.Abstractions;
using EventDriven.EventBus.Dapr.EventCache.Mongo;
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
    /// Add Dapr Mongo event cache to the provided <see cref="T:IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="T:IServiceCollection" /></param>
    /// <param name="configuration">The application's <see cref="IConfiguration"/>.</param>
    /// <returns>The original <see cref="T:IServiceCollection" />.</returns>
    public static IServiceCollection AddDaprMongoEventCache(this IServiceCollection services,
        IConfiguration configuration)
    {
        var daprEventCacheOptions = new DaprEventCacheOptions
        {
            DaprEventCacheType = DaprEventCacheType.Queryable,
            EnableEventCacheCleanup = true,
        };
        var daprOptionsConfigSection = configuration.GetSection(nameof(DaprEventCacheOptions));
        daprOptionsConfigSection.Bind(daprEventCacheOptions);
        if (!daprOptionsConfigSection.Exists())
            throw new Exception($"Configuration section '{nameof(DaprEventCacheOptions)}' not present in app settings.");

        services.Configure<DaprEventCacheOptions>(options =>
        {
            options.DaprEventCacheType = daprEventCacheOptions.DaprEventCacheType;
            options.DaprStateStoreOptions = daprEventCacheOptions.DaprStateStoreOptions;
            options.EnableEventCache = daprEventCacheOptions.EnableEventCache;
            options.EventCacheTimeout = daprEventCacheOptions.EventCacheTimeout;
            options.EnableEventCacheCleanup = daprEventCacheOptions.EnableEventCacheCleanup;
            options.EventCacheCleanupInterval = daprEventCacheOptions.EventCacheCleanupInterval;
        });

        services.AddSingleton<IEventCache, DaprEventCache>();
        services.AddSingleton<IEventHandlingRepository<DaprIntegrationEvent>,
            MongoEventHandlingRepository<DaprIntegrationEvent>>();
        services.AddSingleton<IDocumentRepository<EventWrapperDto>, DocumentRepository<EventWrapperDto>>();
        return services.AddMongoDbSettings<DaprStoreDatabaseSettings, EventWrapperDto>(configuration);
    }

    /// <summary>
    /// Add Dapr Mongo event cache to the provided <see cref="T:IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="T:IServiceCollection" /></param>
    /// <param name="stateStoreName">The name of the state store component to use.</param>
    /// <param name="configureDaprStoreOptions">Configure Dapr store options.</param>
    /// <param name="configureEventCacheOptions">Configure event cache options.</param>
    /// <returns>The original <see cref="T:IServiceCollection" />.</returns>
    public static IServiceCollection AddDaprMongoEventCache(this IServiceCollection services,
        string stateStoreName, Action<DaprStoreDatabaseSettings>? configureDaprStoreOptions = null,
        Action<DaprEventCacheOptions>? configureEventCacheOptions = null)
    {
        var daprEventCacheOptions = new DaprEventCacheOptions
        {
            EnableEventCache = true,
            DaprEventCacheType = DaprEventCacheType.Queryable,
            EnableEventCacheCleanup = true,
            DaprStateStoreOptions = new DaprStateStoreOptions { StateStoreName = stateStoreName }
        };

        if (configureEventCacheOptions != null)
            configureEventCacheOptions(daprEventCacheOptions);
        services.Configure<DaprEventCacheOptions>(options =>
        {
            options.DaprEventCacheType = daprEventCacheOptions.DaprEventCacheType;
            options.DaprStateStoreOptions = daprEventCacheOptions.DaprStateStoreOptions;
            options.EnableEventCache = daprEventCacheOptions.EnableEventCache;
            options.EventCacheTimeout = daprEventCacheOptions.EventCacheTimeout;
            options.EnableEventCacheCleanup = daprEventCacheOptions.EnableEventCacheCleanup;
            options.EventCacheCleanupInterval = daprEventCacheOptions.EventCacheCleanupInterval;
        });

        services.AddSingleton<IEventCache, DaprEventCache>();
        services.AddSingleton<IEventHandlingRepository<DaprIntegrationEvent>,
            MongoEventHandlingRepository<DaprIntegrationEvent>>();
        services.AddSingleton<IDocumentRepository<EventWrapperDto>, DocumentRepository<EventWrapperDto>>();
        return services.AddDaprStoreDatabaseSettings(configureDaprStoreOptions);
    }

    private static IServiceCollection AddDaprStoreDatabaseSettings(this IServiceCollection services,
        Action<DaprStoreDatabaseSettings>? configureDaprStoreOptions = null)
    {
        var databaseSettings = new DaprStoreDatabaseSettings();
        if (configureDaprStoreOptions != null)
        {
            configureDaprStoreOptions(databaseSettings);
            services.Configure(configureDaprStoreOptions);
        }
        return services.AddSingleton(_ =>
        {
            var client = new MongoClient(databaseSettings.ConnectionString);
            var database = client.GetDatabase(databaseSettings.DatabaseName);
            return database.GetCollection<EventWrapperDto>(databaseSettings.CollectionName);
        });
    }
}