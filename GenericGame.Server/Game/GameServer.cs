using LiteNetLib;
using GenericGame;
using GenericGame.Shared.Models;
using GenericGame.Server;
using GenericGame.Shared.Networking;
using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;
using System.Text.Json;

namespace GenericGame.Server;

/// <summary>
/// Listener for NetManager that handles incoming messages
/// </summary>
public class ServerNetEventListener : INetEventListener
{
    private readonly GameServer _server;

    public ServerNetEventListener(GameServer server)
    {
        _server = server;
    }

    public void OnPeerConnected(NetPeer peer)
    {
        _server.AddConnection(peer.Id, peer);
        
        // Send welcome message to the newly connected client
        var welcomeMessage = new ServerWelcomeMessage
        {
            ServerId = Guid.NewGuid(),
            PlayerId = Guid.Empty,  // Will be assigned when client sends lobby join
            ServerMessage = "Welcome to the server!"
        };
        _server.SendJsonToClient(peer.Id, welcomeMessage);
        
        // Broadcast updated connected clients list to all clients
        var connectedClientsMessage = new ConnectedClientsMessage { Clients = _server.GetConnectedClients() };
        var connectedClientsData = NetMessageSerializer.Serialize(connectedClientsMessage);
        _server.Broadcast(connectedClientsData);
        // Note: Connected clients list is broadcast after lobby join (in HandleLobbyJoin)
        // because the player name is not yet known at this point
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        _server.RemoveConnection(peer.Id);
        
        // Broadcast updated connected clients list
        var connectedClientsMessage = new ConnectedClientsMessage { Clients = _server.GetConnectedClients() };
        var connectedClientsData = NetMessageSerializer.Serialize(connectedClientsMessage);
        _server.Broadcast(connectedClientsData);
        
        // Broadcast updated players list
        var playersListMessage = new PlayersListMessage { Players = _server.GetConnectedPlayers() };
        var playersListData = NetMessageSerializer.Serialize(playersListMessage);
        _server.Broadcast(playersListData);
    }

    public void OnNetworkError(IPEndPoint endPoint, SocketError socketErrorCode)
    {
        // Do nothing
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
    {
        try
        {
            var data = reader.GetRemainingBytes();
            Log.Write($"OnNetworkReceive: Received {data.Length} bytes from peer {peer.Id}");
            _server.HandleMessage(peer.Id, data);
        }
        finally
        {
            reader.Recycle();
        }
    }

    public void OnNetworkReceiveUnconnected(IPEndPoint endPoint, NetPacketReader reader, UnconnectedMessageType messageType)
    {
        try
        {
            var data = reader.GetRemainingBytes();
            _server.HandleMessage(0, data);
        }
        finally
        {
            reader.Recycle();
        }
    }

    public void OnConnectionRequest(ConnectionRequest request)
    {
        // Accept the connection
        request.Accept();
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
    private readonly ServerNetEventListener _listener;
    private readonly ConcurrentDictionary<long, PlayerConnection> _connections = new();
    private readonly Dictionary<Guid, Player> _players = new();
    private readonly Dictionary<Guid, GameInstance> _games = new();
    private readonly object _lock = new();

    /// <summary>
    /// Port the server listens on
    /// </summary>
    public int Port { get; }

    /// <summary>
    /// Creates a new GameServer instance
    /// </summary>
    /// <param name="port">Port to listen on</param>
    public GameServer(int port = 14000)
    {
        Port = port;
        _listener = new ServerNetEventListener(this);
        _server = new NetManager(_listener);
        _server.Start(port);
        Log.Write($"Server started on port {port}");
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
    /// Adds a connection to the server
    /// </summary>
    public void AddConnection(long connectionId, NetPeer peer)
    {
        var conn = new PlayerConnection
        {
            Peer = peer,
            PlayerId = Guid.Empty,
            PlayerName = string.Empty,
            IsObserver = false
        };
        _connections.TryAdd(connectionId, conn);
    }

    /// <summary>
    /// Removes a connection from the server
    /// </summary>
    public bool RemoveConnection(long connectionId)
    {
        return _connections.TryRemove(connectionId, out _);
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

    /// <summary>
    /// Gets the player connection for a given connection ID
    /// </summary>
    public bool TryGetConnection(long connectionId, out PlayerConnection connection)
    {
        return _connections.TryGetValue(connectionId, out connection);
    }

    /// <summary>
    /// Gets the player connection for a given player ID
    /// </summary>
    public PlayerConnection? GetConnectionByPlayerId(Guid playerId)
    {
        return _connections.Values.FirstOrDefault(c => c.PlayerId == playerId);
    }

    /// <summary>
    /// Gets all connected clients (not necessarily players who have joined the lobby)
    /// </summary>
    public List<ConnectedClient> GetConnectedClients()
    {
        return _connections.Values.Select(c => new ConnectedClient
        {
            ConnectionId = c.Peer?.Id ?? 0,
            PlayerId = c.PlayerId,
            PlayerName = c.PlayerName,
            IsObserver = c.IsObserver
        }).ToList();
    }
    
    /// <summary>
    /// Gets all connected players
    /// </summary>
    public List<Player> GetConnectedPlayers()
    {
        return _connections.Values.Select(c => new Player
        {
            Id = c.PlayerId,
            Name = c.PlayerName,
            IsObserver = c.IsObserver,
            IsConnected = true
        }).ToList();
    }

    /// <summary>
    /// Gets the player for a given connection ID
    /// </summary>
    public Player? GetPlayer(long connectionId)
    {
        if (_connections.TryGetValue(connectionId, out var conn))
        {
            return new Player
            {
                Id = conn.PlayerId,
                Name = conn.PlayerName,
                IsObserver = conn.IsObserver,
                IsConnected = true
            };
        }
        return null;
    }

    /// <summary>
    /// Gets the player for a given player ID
    /// </summary>
    public Player? GetPlayer(Guid playerId)
    {
        var conn = _connections.Values.FirstOrDefault(c => c.PlayerId == playerId);
        if (conn != null)
        {
            return new Player
            {
                Id = conn.PlayerId,
                Name = conn.PlayerName,
                IsObserver = conn.IsObserver,
                IsConnected = true
            };
        }
        return null;
    }

    /// <summary>
    /// Sends a message to a specific player
    /// </summary>
    public void SendToPlayer(Guid playerId, byte[] data, DeliveryMethod method = DeliveryMethod.Unreliable)
    {
        var conn = GetConnectionByPlayerId(playerId);
        if (conn != null && conn.Peer != null)
        {
            conn.Peer.Send(data, method);
        }
    }

    /// <summary>
    /// Sends a JSON message to a specific player
    /// </summary>
    public void SendJsonToPlayer(Guid playerId, object message)
    {
        var data = NetMessageSerializer.Serialize(message);
        SendToPlayer(playerId, data);
    }

    /// <summary>
    /// Broadcasts a message to all players in a specific game
    /// </summary>
    public void BroadcastToGame(Guid gameId, byte[] data, DeliveryMethod method = DeliveryMethod.Unreliable)
    {
        var game = GetGame(gameId);
        if (game == null) return;

        foreach (var player in game.State.Players)
        {
            SendToPlayer(player.Id, data, method);
        }
    }

    /// <summary>
    /// Broadcasts a JSON message to all players in a specific game
    /// </summary>
    public void BroadcastJsonToGame(Guid gameId, object message)
    {
        var data = NetMessageSerializer.Serialize(message);
        BroadcastToGame(gameId, data);
    }

    /// <summary>
    /// Broadcasts a message to all players except the sender
    /// </summary>
    public void BroadcastToAllExceptSender(long connectionId, byte[] data, DeliveryMethod method = DeliveryMethod.Unreliable)
    {
        foreach (var conn in _connections.Values)
        {
            if (conn.Peer != null && conn.Peer.Id != connectionId)
            {
                conn.Peer.Send(data, method);
            }
        }
    }

    /// <summary>
    /// Broadcasts a JSON message to all players except the sender
    /// </summary>
    public void BroadcastJsonToAllExceptSender(long connectionId, object message)
    {
        var data = NetMessageSerializer.Serialize(message);
        BroadcastToAllExceptSender(connectionId, data);
    }

    /// <summary>
    /// Handles a message received from a client
    /// </summary>
    public void HandleMessage(long connectionId, byte[] data)
    {
        Log.Write("HandleMessage(): enter");
        try
        {
            var message = NetMessageSerializer.Deserialize<Dictionary<string, object>>(data);
            if (message == null) return;

            if (message.TryGetValue("messageType", out var typeObj) && typeObj != null)
            {
                var messageType = typeObj switch
                {
                    byte b => b,
                    int i => (byte)i,
                    JsonElement elem => elem.GetByte(),
                    _ => Convert.ToByte(typeObj.ToString())
                };

                switch (messageType)
                {
                    case NetMessageType.CreateGame:
                        HandleCreateGame(connectionId, data);
                        break;
                    case NetMessageType.InvitePlayer:
                        HandleInvitePlayer(connectionId, data);
                        break;
                    case NetMessageType.JoinGame:
                        HandleJoinGame(connectionId, data);
                        break;
                    case NetMessageType.LeaveGame:
                        HandleLeaveGame(connectionId, data);
                        break;
                    case NetMessageType.LobbyJoin:
                        HandleLobbyJoin(connectionId, data);
                        break;
                    case NetMessageType.LobbyLeave:
                        HandleLobbyLeave(connectionId, data);
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Write($"Error handling message: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles a create game message
    /// </summary>
    private void HandleCreateGame(long connectionId, byte[] data)
    {
        var message = NetMessageSerializer.Deserialize<CreateGameMessage>(data);
        if (message == null) return;

        var game = CreateGame(message.GameName, message.CreatorId);

        // Add invited players to the game
        foreach (var invitedId in message.InvitedPlayerIds)
        {
            var invitedPlayer = GetPlayer(invitedId);
            if (invitedPlayer != null)
            {
                game.AddPlayer(invitedPlayer);
            }
        }

        // Notify all players of the new game
        var gamesListMessage = new GamesListMessage { Games = GetGamesInfo() };
        var gamesListData = NetMessageSerializer.Serialize(gamesListMessage);
        Broadcast(gamesListData);

        // Notify the creator
        SendJsonToPlayer(message.CreatorId, new CreateGameResponseMessage
        {
            Success = true,
            GameId = game.GameId,
            GameState = game.State
        });
    }

    /// <summary>
    /// Handles an invite player message
    /// </summary>
    private void HandleInvitePlayer(long connectionId, byte[] data)
    {
        var message = NetMessageSerializer.Deserialize<InvitePlayerMessage>(data);
        if (message == null) return;

        var game = GetGame(message.GameId);
        if (game == null) return;

        var invitee = GetPlayer(message.InviteeId);
        if (invitee == null) return;

        // Add invitee to game if not already in it
        if (!game.State.Players.Any(p => p.Id == message.InviteeId))
        {
            game.AddPlayer(invitee);
        }

        // Send invitation to invitee
        SendJsonToPlayer(message.InviteeId, new GameInvitationMessage
        {
            GameId = message.GameId,
            GameName = game.Name,
            InviterId = message.InviterId,
            InviterName = GetPlayer(message.InviterId)?.Name ?? "Unknown"
        });

        // Notify all players in the game of the new player
        var gameStateData = NetMessageSerializer.Serialize(new GameUpdateMessage { GameState = game.State });
        BroadcastToGame(message.GameId, gameStateData);
    }

    /// <summary>
    /// Handles a join game message
    /// </summary>
    private void HandleJoinGame(long connectionId, byte[] data)
    {
        var message = NetMessageSerializer.Deserialize<JoinGameMessage>(data);
        if (message == null) return;

        var game = GetGame(message.GameId);
        if (game == null) return;

        var player = GetPlayer(message.PlayerId);
        if (player == null) return;

        // Add player to game if not already in it
        if (!game.State.Players.Any(p => p.Id == message.PlayerId))
        {
            game.AddPlayer(player);
        }

        // Notify all players in the game
        var gameStateData = NetMessageSerializer.Serialize(new GameUpdateMessage { GameState = game.State });
        BroadcastToGame(message.GameId, gameStateData);

        // Check if game is ready to start
        CheckAndStartGame(game);
    }

    /// <summary>
    /// Checks if a game is ready to start and starts it if so
    /// </summary>
    private void CheckAndStartGame(GameInstance game)
    {
        // Try to get the IsReadyToStart method from TicTacToeGame
        var gameType = game.GetType();
        var isReadyMethod = gameType.GetMethod("IsReadyToStart");
        if (isReadyMethod != null)
        {
            var isReady = (bool)isReadyMethod.Invoke(game, null)!;
            if (isReady)
            {
                var startMethod = gameType.GetMethod("StartGame");
                startMethod?.Invoke(game, null);
            }
        }
    }

    /// <summary>
    /// Handles a leave game message
    /// </summary>
    private void HandleLeaveGame(long connectionId, byte[] data)
    {
        var message = NetMessageSerializer.Deserialize<LeaveGameMessage>(data);
        if (message == null) return;

        var game = GetGame(message.GameId);
        if (game == null) return;

        game.RemovePlayer(message.PlayerId);

        // Notify all players in the game
        var gameStateData = NetMessageSerializer.Serialize(new GameUpdateMessage { GameState = game.State });
        BroadcastToGame(message.GameId, gameStateData);
    }

    /// <summary>
    /// Handles a lobby join message
    /// </summary>
    private void HandleLobbyJoin(long connectionId, byte[] data)
    {
        var message = NetMessageSerializer.Deserialize<LobbyJoinMessage>(data);
        if (message == null) return;

        // Update connection info
        if (_connections.TryGetValue(connectionId, out var conn))
        {
            conn.PlayerName = message.PlayerName;
            conn.IsObserver = message.IsObserver;
            if (conn.PlayerId == Guid.Empty)
            {
                conn.PlayerId = Guid.NewGuid(); // Assign a new player ID if not already assigned
            }
            Log.Write($"HandleLobbyJoin: Player joined - {conn.PlayerName} (ID: {conn.PlayerId})");
        }

        // Broadcast updated connected clients list
        var connectedClientsMessage = new ConnectedClientsMessage { Clients = GetConnectedClients() };
        var connectedClientsData = NetMessageSerializer.Serialize(connectedClientsMessage);
        Broadcast(connectedClientsData);
        
        // Broadcast updated players list
        var playersListMessage = new PlayersListMessage { Players = GetConnectedPlayers() };
        var playersListData = NetMessageSerializer.Serialize(playersListMessage);
        Log.Write($"HandleLobbyJoin: Broadcasting players list with {playersListMessage.Players.Count} players");
        Broadcast(playersListData);
        
        // Broadcast updated games list
        var gamesListMessage = new GamesListMessage { Games = GetGamesInfo() };
        var gamesListData = NetMessageSerializer.Serialize(gamesListMessage);
        Broadcast(gamesListData);
    }
    
    /// <summary>
    /// Handles a lobby leave message
    /// </summary>
    private void HandleLobbyLeave(long connectionId, byte[] data)
    {
        var message = NetMessageSerializer.Deserialize<LobbyLeaveMessage>(data);
        if (message == null) return;
        
        // Remove connection
        _connections.TryRemove(connectionId, out _);
        
        // Broadcast updated connected clients list
        var connectedClientsMessage = new ConnectedClientsMessage { Clients = GetConnectedClients() };
        var connectedClientsData = NetMessageSerializer.Serialize(connectedClientsMessage);
        Broadcast(connectedClientsData);
        
        // Broadcast updated players list
        var playersListMessage = new PlayersListMessage { Players = GetConnectedPlayers() };
        var playersListData = NetMessageSerializer.Serialize(playersListMessage);
        Broadcast(playersListData);
        
        // Broadcast updated games list
        var gamesListMessage = new GamesListMessage { Games = GetGamesInfo() };
        var gamesListData = NetMessageSerializer.Serialize(gamesListMessage);
        Broadcast(gamesListData);
    }

    /// <summary>
    /// Gets the list of games as GameInfo objects
    /// </summary>
    private List<GameInfo> GetGamesInfo()
    {
        return _games.Values.Select(g => new GameInfo
        {
            GameId = g.GameId,
            GameName = g.Name,
            CreatorId = g.CreatorId,
            CreatorName = GetPlayer(g.CreatorId)?.Name ?? "Unknown",
            PlayerCount = g.State.Players.Count,
            MaxPlayers = GameState.MaxPlayers,
            IsAiEnabled = false,
            IsStarted = g.State.Status != GameStatus.Lobby
        }).ToList();
    }
}

/// <summary>
/// Response message for create game
/// </summary>
public class CreateGameResponseMessage
{
    public bool Success { get; set; }
    public Guid GameId { get; set; }
    public GameState GameState { get; set; } = new GameState();
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
