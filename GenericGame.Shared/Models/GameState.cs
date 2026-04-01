using System.Collections.ObjectModel;

namespace GenericGame.Shared.Models;

/// <summary>
/// Base class for game state that is shared between server and client
/// </summary>
public class GameState
{
    /// <summary>
    /// Unique identifier for this game instance
    /// </summary>
    public Guid GameId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Current game state (Lobby, Playing, Finished)
    /// </summary>
    public GameStatus Status { get; set; } = GameStatus.Lobby;

    /// <summary>
    /// Current turn number
    /// </summary>
    public int TurnNumber { get; set; }

    /// <summary>
    /// Players in the game
    /// </summary>
    public ObservableCollection<Player> Players { get; set; } = new ObservableCollection<Player>();

    /// <summary>
    /// Current player index whose turn it is
    /// </summary>
    public int CurrentPlayerIndex { get; set; }

    /// <summary>
    /// Maximum number of players
    /// </summary>
    public const int MaxPlayers = 6;

    /// <summary>
    /// Maximum number of observers
    /// </summary>
    public const int MaxObservers = 6;

    /// <summary>
    /// Creates a deep copy of this game state
    /// </summary>
    public virtual GameState Clone()
    {
        var clone = new GameState
        {
            GameId = this.GameId,
            Status = this.Status,
            TurnNumber = this.TurnNumber,
            CurrentPlayerIndex = this.CurrentPlayerIndex
        };

        foreach (var player in this.Players)
        {
            clone.Players.Add(new Player
            {
                Id = player.Id,
                Name = player.Name,
                Index = player.Index,
                IsObserver = player.IsObserver,
                IsConnected = player.IsConnected,
                Endpoint = player.Endpoint
            });
        }

        return clone;
    }
}

/// <summary>
/// Current status of the game
/// </summary>
public enum GameStatus
{
    /// <summary>
    /// Game is in the lobby, waiting for players
    /// </summary>
    Lobby,

    /// <summary>
    /// Game is currently in progress
    /// </summary>
    Playing,

    /// <summary>
    /// Game has finished
    /// </summary>
    Finished
}
