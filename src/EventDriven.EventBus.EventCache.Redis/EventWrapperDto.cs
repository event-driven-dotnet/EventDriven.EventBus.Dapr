namespace EventDriven.EventBus.EventCache.Redis;

/// <summary>
/// Event wrapper DTO.
/// </summary>
public class EventWrapperDto
{
    /// <summary>
    /// Event wrapper identifier.
    /// </summary>
    public string Id { get; set; } = null!;

    /// <summary>
    /// Event wrapper value.
    /// </summary>
    public string Value { get; set; } = null!;
}