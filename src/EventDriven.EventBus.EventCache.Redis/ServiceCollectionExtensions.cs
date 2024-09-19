using EventDriven.EventBus.Abstractions;
using EventDriven.EventBus.EventCache.Redis;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods for <see cref="T:IServiceCollection" />.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add Redis event cache to the provided <see cref="T:IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="T:IServiceCollection" /></param>
    /// <param name="configuration">The application's <see cref="IConfiguration"/>.</param>
    /// <param name="lifetime">Service lifetime.</param>
    /// <returns>The original <see cref="T:IServiceCollection" />.</returns>
    public static IServiceCollection AddRedisEventCache(this IServiceCollection services,
        IConfiguration configuration, ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        var eventCacheOptions = new EventCacheOptions
        {
            EnableEventCacheCleanup = true,
        };
        var redisOptionsConfigSection = configuration.GetSection("RedisEventCacheOptions");
        redisOptionsConfigSection.Bind(eventCacheOptions);
        if (!redisOptionsConfigSection.Exists())
            throw new Exception("Configuration section 'RedisEventCacheOptions' not present in app settings.");
        if (eventCacheOptions.EnableEventCache && string.IsNullOrWhiteSpace(eventCacheOptions.AppName))
            throw new Exception("Configuration section 'RedisEventCacheOptions:AppName' must be specified.");

        if (!eventCacheOptions.EnableEventCache) return services;

        const string name = "RedisEventCacheSettings:DistributedCacheEntryOptions";
        var cacheOptionsSection = configuration.GetSection(name);
        if (!cacheOptionsSection.Exists())
            throw new Exception("Configuration section '" + name + "' not present in app settings.");
        services.Configure<DistributedCacheEntryOptions>(cacheOptionsSection);

        services.AddEventCacheImpl(eventCacheOptions, lifetime);
        return services.AddStackExchangeRedisCache(option =>
        {
            var redisSettingsSection = configuration.GetSection(nameof(RedisEventCacheSettings));
            var redisSettings = redisSettingsSection.Get<RedisEventCacheSettings>();
            option.Configuration = redisSettings?.ConnectionString;
            option.InstanceName = redisSettings?.InstanceName;
        });
    }

    /// <summary>
    /// Add Redis event cache to the provided <see cref="T:IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="T:IServiceCollection" /></param>
    /// <param name="configureEventCacheOptions">Configure event cache options.</param>
    /// <param name="configureRedisCacheOptions">Configure Redis cache options.</param>
    /// <param name="lifetime">Service lifetime.</param>
    /// <returns>The original <see cref="T:IServiceCollection" />.</returns>
    public static IServiceCollection AddRedisEventCache(this IServiceCollection services,
        Action<EventCacheOptions>? configureEventCacheOptions = null,
        Action<RedisEventCacheSettings>? configureRedisCacheOptions = null,
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

        var redisCacheOptions = new RedisEventCacheSettings();
        configureRedisCacheOptions?.Invoke(redisCacheOptions);

        services.Configure<DistributedCacheEntryOptions>(options => 
            options.SlidingExpiration = redisCacheOptions.DistributedCacheEntryOptions.SlidingExpiration);

        return services.AddStackExchangeRedisCache(
            options => options.Configuration = redisCacheOptions.ConnectionString);
    }

    private static IServiceCollection AddEventCacheImpl(this IServiceCollection services,
        EventCacheOptions eventCacheOptions, ServiceLifetime lifetime)
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
                services.AddTransient<IEventCache, RedisEventCache>();
                services.AddTransient<IEventHandlingRepository<IntegrationEvent>,
                    RedisEventHandlingRepository<IntegrationEvent>>();
                services.AddTransient(sp => sp.GetRequiredService<IOptions<DistributedCacheEntryOptions>>().Value);
                break;
            case ServiceLifetime.Scoped:
                services.AddScoped<EventCacheOptions>();
                services.AddScoped<IEventCache, RedisEventCache>();
                services.AddScoped<IEventHandlingRepository<IntegrationEvent>,
                    RedisEventHandlingRepository<IntegrationEvent>>();
                services.AddScoped(sp => sp.GetRequiredService<IOptions<DistributedCacheEntryOptions>>().Value);
                break;
            default:
                services.AddSingleton(eventCacheOptions);
                services.AddSingleton<IEventCache, RedisEventCache>();
                services.AddSingleton<IEventHandlingRepository<IntegrationEvent>,
                    RedisEventHandlingRepository<IntegrationEvent>>();
                services.AddSingleton(sp => sp.GetRequiredService<IOptions<DistributedCacheEntryOptions>>().Value);
                break;
        }

        return services;
    }
}