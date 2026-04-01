using LiteNetLib;
using GenericGame.Shared.Models;
using GenericGame.Server;
using System.Net;
using System.Net.Sockets;

namespace GenericGame.Server;

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
/// Main game server class that handles network connections and game state
/// </summary>
public class GameServer
{
    private readonly NetManager _server;
    private readonly DummyNetEventListener _listener;
    private readonly Dictionary<long, PlayerConnection> _connections = new();
    private readonly Dictionary<Guid, Player> _players = new();
    private readonly Dictionary<Guid, GameInstance> _games = new();
    private readonly object _lock = new();

    /// <summary>
    /// Port the server listens on
    /// </summary>
    public int Port { get; }

    /// <summary>
    /// Current game state
    /// </summary>
    public GameState GameState { get; private set; } = new GameState();

    /// <summary>
    /// Creates a new GameServer instance
    /// </summary>
    /// <param name="port">Port to listen on</param>
    public GameServer(int port = 14000)
    {
        Port = port;
        _listener = new DummyNetEventListener();
        _server = new NetManager(_listener);
        _server.Start(port);
        Console.WriteLine($"Server started on port {port}");
    }

    /// <summary>
    /// Starts the server's update loop
    /// </summary>
    public void Run()
    {
        while (true)
        {
            _server.PollEvents();
            Thread.Sleep(10);
        }
    }

    /// <summary>
    /// Creates a new game instance
    /// </summary>
    public GameInstance CreateGame(string gameName, Guid creatorId)
    {
        lock (_lock)
        {
            var game = new GameInstance(gameName, creatorId);
            _games[game.GameId] = game;
            return game;
        }
    }

    /// <summary>
    /// Gets a game instance by ID
    /// </summary>
    public GameInstance? GetGame(Guid gameId)
    {
        lock (_lock)
        {
            return _games.TryGetValue(gameId, out var game) ? game : null;
        }
    }

    /// <summary>
    /// Gets all active games
    /// </summary>
    public List<GameInstance> GetGames()
    {
        lock (_lock)
        {
            return _games.Values.ToList();
        }
    }

    /// <summary>
    /// Sends a message to a specific client
    /// </summary>
    public void SendToClient(long connectionId, byte[] data, DeliveryMethod method = DeliveryMethod.Unreliable)
    {
        var peer = GetPeer(connectionId);
        peer?.Send(data, method);
    }

    /// <summary>
    /// Broadcasts a message to all connected clients
    /// </summary>
    public void Broadcast(byte[] data, DeliveryMethod method = DeliveryMethod.Unreliable)
    {
        _server.SendToAll(data, method);
    }

    private NetPeer? GetPeer(long connectionId)
    {
        lock (_lock)
        {
            return _connections.TryGetValue(connectionId, out var conn) ? conn.Peer : null;
        }
    }

    /// <summary>
    /// Sends a JSON message to a specific client
    /// </summary>
    public void SendJsonToClient(long connectionId, object message)
    {
        var data = NetMessageSerializer.Serialize(message);
        SendToClient(connectionId, data);
    }

    /// <summary>
    /// Broadcasts a JSON message to all connected clients
    /// </summary>
    public void BroadcastJson(object message)
    {
        var data = NetMessageSerializer.Serialize(message);
        Broadcast(data);
    }

    /// <summary>
    /// Updates the server and processes incoming messages
    /// </summary>
    public void Update()
    {
        _server.PollEvents();
    }
}

/// <summary>
/// Represents a connected player's connection information
/// </summary>
public class PlayerConnection
{
    public NetPeer? Peer { get; set; }
    public Guid PlayerId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public bool IsObserver { get; set; }
}
