using LiteNetLib;
using GenericGame.Shared.Models;
using GenericGame.Client;
using GenericGame.Shared.Networking;
using Action = GenericGame.Shared.Models.Action;
using System.Net;
using System.Net.Sockets;

namespace GenericGame.Client;

/// <summary>
/// Dummy listener for NetManager
/// </summary>
public class DummyNetEventListener : INetEventListener
{
    public void OnPeerConnected(NetPeer peer)
    {
        // Do nothing
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        // Do nothing
    }

    public void OnNetworkError(IPEndPoint endPoint, SocketError socketErrorCode)
    {
        // Do nothing
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
    {
        // Do nothing
    }

    public void OnNetworkReceiveUnconnected(IPEndPoint endPoint, NetPacketReader reader, UnconnectedMessageType messageType)
    {
        // Do nothing
    }

    public void OnConnectionRequest(ConnectionRequest request)
    {
        // Do nothing
    }

    public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
    {
        // Do nothing
    }
}

/// <summary>
/// Main game client class that handles network connections to the server
/// </summary>
public class GameClient
{
    private readonly NetManager _client;
    private readonly DummyNetEventListener _listener;
    private bool _isConnected = false;
    private Guid _playerId = Guid.Empty;

    /// <summary>
    /// Current game state
    /// </summary>
    public GameState GameState { get; private set; } = new GameState();

    /// <summary>
    /// Current player
    /// </summary>
    public Player? CurrentPlayer { get; private set; }

    /// <summary>
    /// Server endpoint
    /// </summary>
    public string ServerAddress { get; private set; } = "localhost";

    /// <summary>
    /// Server port
    /// </summary>
    public int ServerPort { get; private set; } = 14000;

    /// <summary>
    /// Creates a new GameClient instance
    /// </summary>
    public GameClient()
    {
        _listener = new DummyNetEventListener();
        _client = new NetManager(_listener);
    }

    /// <summary>
    /// Connects to the server
    /// </summary>
    /// <param name="address">Server IP address or hostname</param>
    /// <param name="port">Server port</param>
    /// <param name="playerName">Player name</param>
    /// <param name="isObserver">Whether this client is an observer</param>
    public void Connect(string address, int port, string playerName, bool isObserver = false)
    {
        ServerAddress = address;
        ServerPort = port;

        _client.Start();
        _client.Connect(address, port, string.Empty);

        Console.WriteLine($"Connecting to {address}:{port} as {playerName}...");
    }

    /// <summary>
    /// Disconnects from the server
    /// </summary>
    public void Disconnect()
    {
        if (_isConnected)
        {
            _client.Stop();
            _isConnected = false;
            Console.WriteLine("Disconnected from server");
        }
    }

    /// <summary>
    /// Sends a player action to the server
    /// </summary>
    public void SendAction(Action action)
    {
        if (!_isConnected || CurrentPlayer == null) return;

        var message = new PlayerActionMessage
        {
            PlayerId = CurrentPlayer.Id,
            Action = action
        };

        var data = NetMessageSerializer.Serialize(message);
        _client.SendToAll(data, DeliveryMethod.Unreliable);
    }

    /// <summary>
    /// Creates a new game
    /// </summary>
    public void CreateGame(string gameName, List<Guid> invitedPlayerIds, bool isAiEnabled, bool isFirstPlayerRandom)
    {
        var message = new CreateGameMessage
        {
            GameName = gameName,
            CreatorId = CurrentPlayer?.Id ?? Guid.Empty,
            InvitedPlayerIds = invitedPlayerIds,
            IsAiEnabled = isAiEnabled,
            IsFirstPlayerRandom = isFirstPlayerRandom
        };

        var data = NetMessageSerializer.Serialize(message);
        _client.SendToAll(data, DeliveryMethod.ReliableOrdered);
    }

    /// <summary>
    /// Invites a player to join a game
    /// </summary>
    public void InvitePlayer(Guid gameId, Guid inviteeId)
    {
        var message = new InvitePlayerMessage
        {
            GameId = gameId,
            InviterId = CurrentPlayer?.Id ?? Guid.Empty,
            InviteeId = inviteeId
        };

        var data = NetMessageSerializer.Serialize(message);
        _client.SendToAll(data, DeliveryMethod.ReliableOrdered);
    }

    /// <summary>
    /// Joins a game
    /// </summary>
    public void JoinGame(Guid gameId, string playerName)
    {
        var message = new JoinGameMessage
        {
            GameId = gameId,
            PlayerId = CurrentPlayer?.Id ?? Guid.Empty,
            PlayerName = playerName
        };

        var data = NetMessageSerializer.Serialize(message);
        _client.SendToAll(data, DeliveryMethod.ReliableOrdered);
    }

    /// <summary>
    /// Leaves the current game
    /// </summary>
    public void LeaveGame(Guid gameId)
    {
        var message = new LeaveGameMessage
        {
            GameId = gameId,
            PlayerId = CurrentPlayer?.Id ?? Guid.Empty
        };

        var data = NetMessageSerializer.Serialize(message);
        _client.SendToAll(data, DeliveryMethod.ReliableOrdered);
    }

    /// <summary>
    /// Updates the client and processes incoming messages
    /// </summary>
    public void Update()
    {
        _client.PollEvents();
    }

    #region Events

    public event EventHandler<GameStateUpdatedEventArgs>? OnGameStateUpdated;
    public event EventHandler<EventReceivedEventArgs>? OnEventReceived;
    public event EventHandler<PlayerJoinedEventArgs>? OnPlayerJoined;
    public event EventHandler<PlayerLeftEventArgs>? OnPlayerLeft;
    public event EventHandler<GameStartedEventArgs>? OnGameStarted;
    public event EventHandler<GameEndedEventArgs>? OnGameEnded;
    public event EventHandler<LobbyUpdateEventArgs>? OnLobbyUpdate;
    public event EventHandler<GamesListUpdateEventArgs>? OnGamesListUpdate;
    public event EventHandler<GameInvitationReceivedEventArgs>? OnGameInvitationReceived;
    public event EventHandler<PlayerInvitedEventArgs>? OnPlayerInvited;

    #endregion

    #region Event Raising Methods

    protected virtual void OnGameStateUpdatedRaise(GameState gameState)
    {
        OnGameStateUpdated?.Invoke(this, new GameStateUpdatedEventArgs(gameState));
    }

    protected virtual void OnEventReceivedRaise(Event @event)
    {
        OnEventReceived?.Invoke(this, new EventReceivedEventArgs(@event));
    }

    protected virtual void OnPlayerJoinedRaise(Player player)
    {
        OnPlayerJoined?.Invoke(this, new PlayerJoinedEventArgs(player));
    }

    protected virtual void OnPlayerLeftRaise(Player player)
    {
        OnPlayerLeft?.Invoke(this, new PlayerLeftEventArgs(player));
    }

    protected virtual void OnGameStartedRaise(GameState gameState)
    {
        OnGameStarted?.Invoke(this, new GameStartedEventArgs(gameState));
    }

    protected virtual void OnGameEndedRaise(GameState gameState)
    {
        OnGameEnded?.Invoke(this, new GameEndedEventArgs(gameState));
    }

    protected virtual void OnLobbyUpdateRaise(List<Player> players, List<GameInfo> games)
    {
        OnLobbyUpdate?.Invoke(this, new LobbyUpdateEventArgs(players, games));
    }

    protected virtual void OnGamesListUpdateRaise(List<GameInfo> games)
    {
        OnGamesListUpdate?.Invoke(this, new GamesListUpdateEventArgs(games));
    }

    protected virtual void OnGameInvitationReceivedRaise(Guid gameId, string gameName, Guid inviterId, string inviterName)
    {
        OnGameInvitationReceived?.Invoke(this, new GameInvitationReceivedEventArgs(gameId, gameName, inviterId, inviterName));
    }

    protected virtual void OnPlayerInvitedRaise(Guid gameId, Guid inviteeId, string inviteeName)
    {
        OnPlayerInvited?.Invoke(this, new PlayerInvitedEventArgs(gameId, inviteeId, inviteeName));
    }

    #endregion
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
/// Event arguments for event received event
/// </summary>
public class EventReceivedEventArgs : EventArgs
{
    public Event Event { get; }

    public EventReceivedEventArgs(Event @event)
    {
        Event = @event;
    }
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

/// <summary>
/// Event arguments for lobby update event
/// </summary>
public class LobbyUpdateEventArgs : EventArgs
{
    public List<Player> Players { get; }
    public List<GameInfo> Games { get; }

    public LobbyUpdateEventArgs(List<Player> players, List<GameInfo> games)
    {
        Players = players;
        Games = games;
    }
}

/// <summary>
/// Event arguments for games list update event
/// </summary>
public class GamesListUpdateEventArgs : EventArgs
{
    public List<GameInfo> Games { get; }

    public GamesListUpdateEventArgs(List<GameInfo> games)
    {
        Games = games;
    }
}

/// <summary>
/// Event arguments for game invitation received event
/// </summary>
public class GameInvitationReceivedEventArgs : EventArgs
{
    public Guid GameId { get; }
    public string GameName { get; }
    public Guid InviterId { get; }
    public string InviterName { get; }

    public GameInvitationReceivedEventArgs(Guid gameId, string gameName, Guid inviterId, string inviterName)
    {
        GameId = gameId;
        GameName = gameName;
        InviterId = inviterId;
        InviterName = inviterName;
    }
}

/// <summary>
/// Event arguments for player invited event
/// </summary>
public class PlayerInvitedEventArgs : EventArgs
{
    public Guid GameId { get; }
    public Guid InviteeId { get; }
    public string InviteeName { get; }

    public PlayerInvitedEventArgs(Guid gameId, Guid inviteeId, string inviteeName)
    {
        GameId = gameId;
        InviteeId = inviteeId;
        InviteeName = inviteeName;
    }
}
