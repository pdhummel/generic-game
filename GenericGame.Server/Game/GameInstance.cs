using GenericGame.Shared.Models;
using Action = GenericGame.Shared.Models.Action;

namespace GenericGame.Server;

/// <summary>
/// Represents a specific game instance with its own state and rules
/// </summary>
public class GameInstance
{
    /// <summary>
    /// Unique identifier for this game
    /// </summary>
    public Guid GameId { get; private set; } = Guid.NewGuid();

    /// <summary>
    /// Name of the game
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// ID of the player who created this game
    /// </summary>
    public Guid CreatorId { get; private set; }

    /// <summary>
    /// Current game state
    /// </summary>
    public GameState State { get; protected set; } = new GameState();

    /// <summary>
    /// Players in this game
    /// </summary>
    private readonly Dictionary<Guid, Player> _players = new();

    /// <summary>
    /// Creates a new game instance
    /// </summary>
    /// <param name="name">Name of the game</param>
    /// <param name="creatorId">ID of the player who created this game</param>
    public GameInstance(string name, Guid creatorId)
    {
        Name = name;
        CreatorId = creatorId;
        State.GameId = GameId;
        State.Status = GameStatus.Lobby;
    }

    /// <summary>
    /// Adds a player to the game
    /// </summary>
    public bool AddPlayer(Player player)
    {
        lock (State)
        {
            if (State.Players.Count >= GameState.MaxPlayers)
                return false;

            if (player.IsObserver && State.Players.Count(p => p.IsObserver) >= GameState.MaxObservers)
                return false;

            player.Index = State.Players.Count;
            State.Players.Add(player);
            _players[player.Id] = player;

            // Notify all players of the new player
            OnPlayerJoinedRaise(player);

            return true;
        }
    }

    /// <summary>
    /// Removes a player from the game
    /// </summary>
    public bool RemovePlayer(Guid playerId)
    {
        lock (State)
        {
            if (_players.TryGetValue(playerId, out var player))
            {
                State.Players.Remove(player);
                _players.Remove(playerId);
                OnPlayerLeftRaise(player);
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Processes a player action
    /// </summary>
    public virtual void ProcessAction(Action action, Guid playerId)
    {
        // Validate that the player is in the game
        if (!_players.TryGetValue(playerId, out var player))
            return;

        // Validate that it's the player's turn (if applicable)
        if (!player.IsObserver &&
            State.CurrentPlayerIndex != player.Index)
        {
            // Not this player's turn
            return;
        }

        // Process the action
        OnActionProcessedRaise(action, playerId);

        // Update turn
        UpdateNextTurn();
    }

    /// <summary>
    /// Updates to the next player's turn
    /// </summary>
    protected virtual void UpdateNextTurn()
    {
        lock (State)
        {
            State.TurnNumber++;
            State.CurrentPlayerIndex = (State.CurrentPlayerIndex + 1) % State.Players.Count(p => !p.IsObserver);
            OnGameStateUpdatedRaise(State.Clone());
        }
    }

    /// <summary>
    /// Starts the game
    /// </summary>
    public virtual void StartGame()
    {
        lock (State)
        {
            State.Status = GameStatus.Playing;
            State.TurnNumber = 1;
            State.CurrentPlayerIndex = 0;
            OnGameStartedRaise(State.Clone());
        }
    }

    /// <summary>
    /// Ends the game
    /// </summary>
    public virtual void EndGame()
    {
        lock (State)
        {
            State.Status = GameStatus.Finished;
            OnGameEndedRaise(State.Clone());
        }
    }

    /// <summary>
    /// Notifies all players of a game state update
    /// </summary>
    protected virtual void NotifyGameStateUpdate()
    {
        OnGameStateUpdatedRaise(State.Clone());
    }

    #region Events

    public event EventHandler<PlayerJoinedEventArgs>? OnPlayerJoined;
    public event EventHandler<PlayerLeftEventArgs>? OnPlayerLeft;
    public event EventHandler<ActionProcessedEventArgs>? OnActionProcessed;
    public event EventHandler<GameStateUpdatedEventArgs>? OnGameStateUpdated;
    public event EventHandler<GameStartedEventArgs>? OnGameStarted;
    public event EventHandler<GameEndedEventArgs>? OnGameEnded;

    #endregion

    #region Event Raising Methods

    protected virtual void OnPlayerJoinedRaise(Player player)
    {
        OnPlayerJoined?.Invoke(this, new PlayerJoinedEventArgs(player));
    }

    protected virtual void OnPlayerLeftRaise(Player player)
    {
        OnPlayerLeft?.Invoke(this, new PlayerLeftEventArgs(player));
    }

    protected virtual void OnActionProcessedRaise(Action action, Guid playerId)
    {
        OnActionProcessed?.Invoke(this, new ActionProcessedEventArgs(action, playerId));
    }

    protected virtual void OnGameStateUpdatedRaise(GameState gameState)
    {
        OnGameStateUpdated?.Invoke(this, new GameStateUpdatedEventArgs(gameState));
    }

    protected virtual void OnGameStartedRaise(GameState gameState)
    {
        OnGameStarted?.Invoke(this, new GameStartedEventArgs(gameState));
    }

    protected virtual void OnGameEndedRaise(GameState gameState)
    {
        OnGameEnded?.Invoke(this, new GameEndedEventArgs(gameState));
    }

    #endregion
}

/// <summary>
/// Event arguments for player joined event
/// </summary>
public class PlayerJoinedEventArgs : EventArgs
{
    public Player Player { get; }

    public PlayerJoinedEventArgs(Player player)
    {
        Player = player;
    }
}

/// <summary>
/// Event arguments for player left event
/// </summary>
public class PlayerLeftEventArgs : EventArgs
{
    public Player Player { get; }

    public PlayerLeftEventArgs(Player player)
    {
        Player = player;
    }
}

/// <summary>
/// Event arguments for action processed event
/// </summary>
public class ActionProcessedEventArgs : EventArgs
{
    public Action Action { get; }
    public Guid PlayerId { get; }

    public ActionProcessedEventArgs(Action action, Guid playerId)
    {
        Action = action;
        PlayerId = playerId;
    }
}

/// <summary>
/// Event arguments for game state updated event
/// </summary>
public class GameStateUpdatedEventArgs : EventArgs
{
    public GameState GameState { get; }

    public GameStateUpdatedEventArgs(GameState gameState)
    {
        GameState = gameState;
    }
}

/// <summary>
/// Event arguments for game started event
/// </summary>
public class GameStartedEventArgs : EventArgs
{
    public GameState GameState { get; }

    public GameStartedEventArgs(GameState gameState)
    {
        GameState = gameState;
    }
}

/// <summary>
/// Event arguments for game ended event
/// </summary>
public class GameEndedEventArgs : EventArgs
{
    public GameState GameState { get; }

    public GameEndedEventArgs(GameState gameState)
    {
        GameState = gameState;
    }
}
