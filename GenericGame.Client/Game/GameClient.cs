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
    /// Joins the lobby
    /// </summary>
    public void JoinLobby(string playerName, bool isObserver = false)
    {
        var message = new LobbyJoinMessage
        {
            PlayerName = playerName,
            IsObserver = isObserver
        };

        var data = NetMessageSerializer.Serialize(message);
        _client.SendToAll(data, DeliveryMethod.ReliableOrdered);
    }

    /// <summary>
    /// Leaves the lobby
    /// </summary>
    public void LeaveLobby()
    {
        var message = new LobbyLeaveMessage
        {
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
