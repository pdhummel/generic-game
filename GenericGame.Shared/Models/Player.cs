namespace GenericGame.Shared.Models;

/// <summary>
/// Represents a player in the game
/// </summary>
public class Player
{
    /// <summary>
    /// Unique identifier for the player
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Player's display name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Player's index (0-5 for 6 players max)
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// Whether the player is an observer (spectator) or active participant
    /// </summary>
    public bool IsObserver { get; set; }

    /// <summary>
    /// Whether the player is connected
    /// </summary>
    public bool IsConnected { get; set; }

    /// <summary>
    /// Client endpoint information
    /// </summary>
    public string? Endpoint { get; set; }
}
