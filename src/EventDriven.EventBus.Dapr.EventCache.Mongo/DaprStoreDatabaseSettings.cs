using EventDriven.DependencyInjection.URF.Mongo;

namespace EventDriven.EventBus.Dapr.EventCache.Mongo;

/// <summary>
/// Dapr store database settings.
/// </summary>
public class DaprStoreDatabaseSettings : IMongoDbSettings
{
    /// <inheritdoc />
    public string ConnectionString { get; set; } = null!;

    /// <inheritdoc />
    public string DatabaseName { get; set; } = null!;

    /// <inheritdoc />
    public string CollectionName { get; set; } = null!;
}