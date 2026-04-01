namespace GenericGame.Shared.Models;

/// <summary>
/// Base class for all player actions that can be sent to the server
/// Actions represent player choices and are processed by the server
/// </summary>
public abstract class Action
{
    /// <summary>
    /// The player who initiated this action
    /// </summary>
    public Guid PlayerId { get; set; }

    /// <summary>
    /// Timestamp when the action was created
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Creates a deep copy of this action
    /// </summary>
    public abstract Action Clone();
}
