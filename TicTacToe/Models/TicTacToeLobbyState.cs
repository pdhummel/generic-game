using GenericGame.Shared.Models;
using GenericGame.Shared.Networking;

namespace TicTacToe.Models;

/// <summary>
/// Tic-Tac-Toe lobby state
/// </summary>
public class TicTacToeLobbyState
{
    /// <summary>
    /// List of connected players in the lobby
    /// </summary>
    public List<Player> Players { get; set; } = new List<Player>();

    /// <summary>
    /// List of available games
    /// </summary>
    public List<GameInfo> Games { get; set; } = new List<GameInfo>();

    /// <summary>
    /// The current player's ID
    /// </summary>
    public Guid CurrentPlayerId { get; set; }

    /// <summary>
    /// Creates a deep copy of this lobby state
    /// </summary>
    public TicTacToeLobbyState Clone()
    {
        return new TicTacToeLobbyState
        {
            Players = new List<Player>(Players),
            Games = new List<GameInfo>(Games),
            CurrentPlayerId = CurrentPlayerId
        };
    }
}

/// <summary>
/// Tic-Tac-Toe event - player invited to a game
/// </summary>
public class PlayerInvitedEvent : Event
{
    /// <summary>
    /// Game ID
    /// </summary>
    public Guid GameId { get; set; }

    /// <summary>
    /// Player ID who was invited
    /// </summary>
    public Guid InviteeId { get; set; }

    /// <summary>
    /// Player name who was invited
    /// </summary>
    public string InviteeName { get; set; } = string.Empty;

    /// <summary>
    /// Creates a deep copy of this event
    /// </summary>
    public override Event Clone()
    {
        return new PlayerInvitedEvent
        {
            Timestamp = Timestamp,
            GameId = GameId,
            InviteeId = InviteeId,
            InviteeName = InviteeName
        };
    }
}

/// <summary>
/// Tic-Tac-Toe event - game list updated
/// </summary>
public class GamesListUpdatedEvent : Event
{
    /// <summary>
    /// List of available games
    /// </summary>
    public List<GameInfo> Games { get; set; } = new List<GameInfo>();

    /// <summary>
    /// Creates a deep copy of this event
    /// </summary>
    public override Event Clone()
    {
        return new GamesListUpdatedEvent
        {
            Timestamp = Timestamp,
            Games = new List<GameInfo>(Games)
        };
    }
}

/// <summary>
/// Tic-Tac-Toe event - players list updated
/// </summary>
public class PlayersListUpdatedEvent : Event
{
    /// <summary>
    /// List of connected players
    /// </summary>
    public List<Player> Players { get; set; } = new List<Player>();

    /// <summary>
    /// Creates a deep copy of this event
    /// </summary>
    public override Event Clone()
    {
        return new PlayersListUpdatedEvent
        {
            Timestamp = Timestamp,
            Players = new List<Player>(Players)
        };
    }
}

/// <summary>
/// Tic-Tac-Toe event - game created
/// </summary>
public class GameCreatedEvent : Event
{
    /// <summary>
    /// Game ID
    /// </summary>
    public Guid GameId { get; set; }

    /// <summary>
    /// Game name
    /// </summary>
    public string GameName { get; set; } = string.Empty;

    /// <summary>
    /// Creator ID
    /// </summary>
    public Guid CreatorId { get; set; }

    /// <summary>
    /// Creator name
    /// </summary>
    public string CreatorName { get; set; } = string.Empty;

    /// <summary>
    /// Is AI enabled
    /// </summary>
    public bool IsAiEnabled { get; set; }

    /// <summary>
    /// Is first player randomly chosen
    /// </summary>
    public bool IsFirstPlayerRandom { get; set; }

    /// <summary>
    /// Creates a deep copy of this event
    /// </summary>
    public override Event Clone()
    {
        return new GameCreatedEvent
        {
            Timestamp = Timestamp,
            GameId = GameId,
            GameName = GameName,
            CreatorId = CreatorId,
            CreatorName = CreatorName,
            IsAiEnabled = IsAiEnabled,
            IsFirstPlayerRandom = IsFirstPlayerRandom
        };
    }
}

/// <summary>
/// Tic-Tac-Toe event - player joined a game
/// </summary>
public class PlayerJoinedGameEvent : Event
{
    /// <summary>
    /// Game ID
    /// </summary>
    public Guid GameId { get; set; }

    /// <summary>
    /// Player ID who joined
    /// </summary>
    public Guid PlayerId { get; set; }

    /// <summary>
    /// Player name who joined
    /// </summary>
    public string PlayerName { get; set; } = string.Empty;

    /// <summary>
    /// Creates a deep copy of this event
    /// </summary>
    public override Event Clone()
    {
        return new PlayerJoinedGameEvent
        {
            Timestamp = Timestamp,
            GameId = GameId,
            PlayerId = PlayerId,
            PlayerName = PlayerName
        };
    }
}

/// <summary>
/// Tic-Tac-Toe event - game ready to start
/// </summary>
public class GameReadyToStartEvent : Event
{
    /// <summary>
    /// Game ID
    /// </summary>
    public Guid GameId { get; set; }

    /// <summary>
    /// Game state
    /// </summary>
    public TicTacToeGameState GameState { get; set; } = new TicTacToeGameState();

    /// <summary>
    /// Creates a deep copy of this event
    /// </summary>
    public override Event Clone()
    {
        return new GameReadyToStartEvent
        {
            Timestamp = Timestamp,
            GameId = GameId,
            GameState = (TicTacToeGameState)GameState.Clone()
        };
    }
}
