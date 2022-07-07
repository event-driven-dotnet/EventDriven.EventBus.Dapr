using MongoDB.Bson.Serialization.Attributes;

namespace EventDriven.EventBus.EventCache.Mongo;

/// <summary>
/// Event wrapper DTO.
/// </summary>
public class EventWrapperDto
{
    /// <summary>
    /// Event wrapper identifier.
    /// </summary>
    [BsonElement("_id")]
    public string Id { get; set; } = null!;

    /// <summary>
    /// Event wrapper etag.
    /// </summary>
    [BsonElement("_etag")]
    public string Etag { get; set; } = null!;

    /// <summary>
    /// Event wrapper value.
    /// </summary>
    [BsonElement("value")]
    public string Value { get; set; } = null!;
}