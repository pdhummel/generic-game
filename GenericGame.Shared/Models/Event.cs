namespace GenericGame.Shared.Models;

/// <summary>
/// Base class for all events published by the server to clients
/// Events represent game state changes and are used to update client visualization
/// </summary>
public abstract class Event
{
    /// <summary>
    /// Timestamp when the event was created
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Creates a deep copy of this event
    /// </summary>
    public abstract Event Clone();
}
