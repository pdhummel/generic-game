using LiteNetLib;
using GenericGame;
using GenericGame.Shared.Models;
using GenericGame.Client;
using GenericGame.Shared.Networking;
using Action = GenericGame.Shared.Models.Action;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace GenericGame.Client;

/// <summary>
/// Listener for NetManager that handles incoming messages
/// </summary>
public class ClientNetEventListener : INetEventListener
{
    private readonly GameClient _client;

    public ClientNetEventListener(GameClient client)
    {
        _client = client;
    }

    public void OnPeerConnected(NetPeer peer)
    {
        // Connection established - client will receive welcome message
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
        try
        {
            var data = reader.GetRemainingBytes();
            Log.Write($"OnNetworkReceive: Received {data.Length} bytes from peer {peer.Id}");
            _client.HandleIncomingMessage(data);
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
            _client.HandleIncomingMessage(data);
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
/// Main game client class that handles network connections to the server
/// </summary>
public class GameClient
{
    private readonly NetManager _client;
    private readonly ClientNetEventListener _listener;
    private bool _isConnected = false;
    private Guid _playerId = Guid.Empty;
    private string _playerName = string.Empty;
    private bool _isObserver = false;

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
        _listener = new ClientNetEventListener(this);
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
        _playerName = playerName;
        _isObserver = isObserver;

        _client.Start();
        _client.Connect(address, port, string.Empty);

        Log.Write($"Connecting to {address}:{port} as {playerName}...");
    }

    /// <summary>
    /// Handles incoming messages from the server
    /// </summary>
    /// <param name="data">The message data</param>
    public void HandleIncomingMessage(byte[] data)
    {
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
                Log.Write($"HandleIncomingMessage: Received message type {messageType}");

                switch (messageType)
                {
                    case NetMessageType.ServerWelcome:
                        HandleServerWelcome(message);
                        break;
                    case NetMessageType.LobbyUpdate:
                        HandleLobbyUpdate(message);
                        break;
                    case NetMessageType.GamesListUpdate:
                        HandleGamesListUpdate(message);
                        break;
                    case NetMessageType.GameUpdate:
                        HandleGameUpdate(message);
                        break;
                    case NetMessageType.PlayerJoined:
                        HandlePlayerJoined(message);
                        break;
                    case NetMessageType.PlayerLeft:
                        HandlePlayerLeft(message);
                        break;
                    case NetMessageType.GameStarted:
                        HandleGameStarted(message);
                        break;
                    case NetMessageType.GameEnded:
                        HandleGameEnded(message);
                        break;
                    case NetMessageType.GameInvitation:
                        HandleGameInvitation(message);
                        break;
                    case NetMessageType.PlayerInvited:
                        HandlePlayerInvited(message);
                        break;
                    case NetMessageType.PlayersListUpdate:
                        HandlePlayersListUpdate(message);
                        break;
                    case NetMessageType.ConnectedClientsUpdate:
                        HandleConnectedClientsUpdate(message);
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
    /// Handles server welcome message
    /// </summary>
    private void HandleServerWelcome(Dictionary<string, object> message)
    {
        Log.Write("HandleServerWelcome: Received welcome message from server");
        // Try camelCase first, then PascalCase
        if (message.TryGetValue("playerId", out var playerIdObj) && playerIdObj != null)
        {
            _playerId = Guid.Parse(playerIdObj.ToString());
            CurrentPlayer = new Player
            {
                Id = _playerId,
                Name = _playerName,
                IsObserver = _isObserver,
                IsConnected = true
            };
            Log.Write($"HandleServerWelcome: Player ID assigned: {_playerId}");
        }
        else if (message.TryGetValue("PlayerId", out playerIdObj) && playerIdObj != null)
        {
            _playerId = Guid.Parse(playerIdObj.ToString());
            CurrentPlayer = new Player
            {
                Id = _playerId,
                Name = _playerName,
                IsObserver = _isObserver,
                IsConnected = true
            };
            Log.Write($"HandleServerWelcome: Player ID assigned: {_playerId}");
        }

        // Send lobby join message to register in the lobby
        Log.Write("HandleServerWelcome: Sending lobby join message");
        SendLobbyJoin();
    }

    /// <summary>
    /// Sends a lobby join message to register in the lobby
    /// </summary>
    private void SendLobbyJoin()
    {
        Log.Write($"SendLobbyJoin: Sending lobby join for player '{_playerName}'");
        var message = new LobbyJoinMessage
        {
            PlayerName = _playerName,
            IsObserver = _isObserver
        };

        var data = NetMessageSerializer.Serialize(message);
        // Send to all peers (should only be the server in client mode)
        _client.SendToAll(data, DeliveryMethod.ReliableOrdered);
        Log.Write("SendLobbyJoin: Lobby join message sent");
    }

    /// <summary>
    /// Handles lobby update message
    /// </summary>
    private void HandleLobbyUpdate(Dictionary<string, object> message)
    {
        var players = new List<Player>();
        var games = new List<GameInfo>();

        // Try camelCase first, then PascalCase
        if (message.TryGetValue("players", out var playersObj) && playersObj != null)
        {
            players = DeserializePlayers(playersObj);
        }
        else if (message.TryGetValue("Players", out playersObj) && playersObj != null)
        {
            players = DeserializePlayers(playersObj);
        }

        if (message.TryGetValue("games", out var gamesObj) && gamesObj != null)
        {
            games = DeserializeGames(gamesObj);
        }
        else if (message.TryGetValue("Games", out gamesObj) && gamesObj != null)
        {
            games = DeserializeGames(gamesObj);
        }

        OnLobbyUpdateRaise(players, games);
    }

    /// <summary>
    /// Handles games list update message
    /// </summary>
    private void HandleGamesListUpdate(Dictionary<string, object> message)
    {
        var games = new List<GameInfo>();

        // Try camelCase first, then PascalCase
        if (message.TryGetValue("games", out var gamesObj) && gamesObj != null)
        {
            games = DeserializeGames(gamesObj);
        }
        else if (message.TryGetValue("Games", out gamesObj) && gamesObj != null)
        {
            games = DeserializeGames(gamesObj);
        }

        OnGamesListUpdateRaise(games);
    }

    /// <summary>
    /// Handles game update message
    /// </summary>
    private void HandleGameUpdate(Dictionary<string, object> message)
    {
        // Try camelCase first, then PascalCase
        if (message.TryGetValue("gameState", out var gameStateObj) && gameStateObj != null)
        {
            var gameState = DeserializeGameState(gameStateObj);
            OnGameStateUpdatedRaise(gameState);
        }
        else if (message.TryGetValue("GameState", out gameStateObj) && gameStateObj != null)
        {
            var gameState = DeserializeGameState(gameStateObj);
            OnGameStateUpdatedRaise(gameState);
        }
    }

    /// <summary>
    /// Handles player joined message
    /// </summary>
    private void HandlePlayerJoined(Dictionary<string, object> message)
    {
        // Try camelCase first, then PascalCase
        if (message.TryGetValue("player", out var playerObj) && playerObj != null)
        {
            var player = DeserializePlayer(playerObj);
            OnPlayerJoinedRaise(player);
        }
        else if (message.TryGetValue("Player", out playerObj) && playerObj != null)
        {
            var player = DeserializePlayer(playerObj);
            OnPlayerJoinedRaise(player);
        }
    }

    /// <summary>
    /// Handles player left message
    /// </summary>
    private void HandlePlayerLeft(Dictionary<string, object> message)
    {
        // Try camelCase first, then PascalCase
        if (message.TryGetValue("playerId", out var playerIdObj) && playerIdObj != null)
        {
            var playerId = Guid.Parse(playerIdObj.ToString());
            var player = new Player { Id = playerId, Name = message.GetValueOrDefault("playerName")?.ToString() ?? "Unknown" };
            OnPlayerLeftRaise(player);
        }
        else if (message.TryGetValue("PlayerId", out playerIdObj) && playerIdObj != null)
        {
            var playerId = Guid.Parse(playerIdObj.ToString());
            var player = new Player { Id = playerId, Name = message.GetValueOrDefault("PlayerName")?.ToString() ?? "Unknown" };
            OnPlayerLeftRaise(player);
        }
    }

    /// <summary>
    /// Handles game started message
    /// </summary>
    private void HandleGameStarted(Dictionary<string, object> message)
    {
        // Try camelCase first, then PascalCase
        if (message.TryGetValue("gameState", out var gameStateObj) && gameStateObj != null)
        {
            var gameState = DeserializeGameState(gameStateObj);
            OnGameStartedRaise(gameState);
        }
        else if (message.TryGetValue("GameState", out gameStateObj) && gameStateObj != null)
        {
            var gameState = DeserializeGameState(gameStateObj);
            OnGameStartedRaise(gameState);
        }
    }

    /// <summary>
    /// Handles game ended message
    /// </summary>
    private void HandleGameEnded(Dictionary<string, object> message)
    {
        // Try camelCase first, then PascalCase
        if (message.TryGetValue("gameState", out var gameStateObj) && gameStateObj != null)
        {
            var gameState = DeserializeGameState(gameStateObj);
            OnGameEndedRaise(gameState);
        }
        else if (message.TryGetValue("GameState", out gameStateObj) && gameStateObj != null)
        {
            var gameState = DeserializeGameState(gameStateObj);
            OnGameEndedRaise(gameState);
        }
    }

    /// <summary>
    /// Handles game invitation message
    /// </summary>
    private void HandleGameInvitation(Dictionary<string, object> message)
    {
        // Try camelCase first, then PascalCase
        if (message.TryGetValue("gameId", out var gameIdObj) && gameIdObj != null &&
            message.TryGetValue("gameName", out var gameNameObj) && gameNameObj != null &&
            message.TryGetValue("inviterId", out var inviterIdObj) && inviterIdObj != null &&
            message.TryGetValue("inviterName", out var inviterNameObj) && inviterNameObj != null)
        {
            var gameId = Guid.Parse(gameIdObj.ToString());
            var gameName = gameNameObj.ToString() ?? string.Empty;
            var inviterId = Guid.Parse(inviterIdObj.ToString());
            var inviterName = inviterNameObj.ToString() ?? string.Empty;

            OnGameInvitationReceivedRaise(gameId, gameName, inviterId, inviterName);
        }
        else if (message.TryGetValue("GameId", out gameIdObj) && gameIdObj != null &&
            message.TryGetValue("GameName", out gameNameObj) && gameNameObj != null &&
            message.TryGetValue("InviterId", out inviterIdObj) && inviterIdObj != null &&
            message.TryGetValue("InviterName", out inviterNameObj) && inviterNameObj != null)
        {
            var gameId = Guid.Parse(gameIdObj.ToString());
            var gameName = gameNameObj.ToString() ?? string.Empty;
            var inviterId = Guid.Parse(inviterIdObj.ToString());
            var inviterName = inviterNameObj.ToString() ?? string.Empty;

            OnGameInvitationReceivedRaise(gameId, gameName, inviterId, inviterName);
        }
    }

    /// <summary>
    /// Handles player invited message
    /// </summary>
    private void HandlePlayerInvited(Dictionary<string, object> message)
    {
        // Try camelCase first, then PascalCase
        if (message.TryGetValue("gameId", out var gameIdObj) && gameIdObj != null &&
            message.TryGetValue("inviteeId", out var inviteeIdObj) && inviteeIdObj != null &&
            message.TryGetValue("InviteeName", out var inviteeNameObj) && inviteeNameObj != null)
        {
            var gameId = Guid.Parse(gameIdObj.ToString());
            var inviteeId = Guid.Parse(inviteeIdObj.ToString());
            var inviteeName = inviteeNameObj.ToString() ?? string.Empty;

            OnPlayerInvitedRaise(gameId, inviteeId, inviteeName);
        }
    }

    /// <summary>
    /// Handles players list update message
    /// </summary>
    private void HandlePlayersListUpdate(Dictionary<string, object> message)
    {
        var players = new List<Player>();

        if (message.TryGetValue("Players", out var playersObj) && playersObj != null)
        {
            players = DeserializePlayers(playersObj);
            Log.Write($"HandlePlayersListUpdate: Received {players.Count} players");
            foreach (var player in players)
            {
                Log.Write($"  - Player: {player.Name} (ID: {player.Id})");
            }
        }
        else
        {
            Log.Write("HandlePlayersListUpdate: No players found in message");
        }

        OnLobbyUpdateRaise(players, new List<GameInfo>());
    }
    
    /// <summary>
    /// Handles connected clients update message
    /// </summary>
    private void HandleConnectedClientsUpdate(Dictionary<string, object> message)
    {
        var clients = new List<ConnectedClient>();
        
        // Try both "clients" (camelCase) and "Clients" (PascalCase)
        if (message.TryGetValue("clients", out var clientsObj) && clientsObj != null)
        {
            clients = DeserializeConnectedClients(clientsObj);
            Log.Write($"HandleConnectedClientsUpdate: Received {clients.Count} connected clients");
            foreach (var client in clients)
            {
                Log.Write($"  - Client: {client.PlayerName} (ID: {client.PlayerId}, ConnectionId: {client.ConnectionId})");
            }
        }
        else if (message.TryGetValue("Clients", out clientsObj) && clientsObj != null)
        {
            clients = DeserializeConnectedClients(clientsObj);
            Log.Write($"HandleConnectedClientsUpdate: Received {clients.Count} connected clients");
            foreach (var client in clients)
            {
                Log.Write($"  - Client: {client.PlayerName} (ID: {client.PlayerId}, ConnectionId: {client.ConnectionId})");
            }
        }
        else
        {
            Log.Write("HandleConnectedClientsUpdate: No clients found in message");
        }
        
        OnConnectedClientsUpdateRaise(clients);
    }
    
    /// <summary>
    /// Deserializes a connected client object
    /// </summary>
    private ConnectedClient DeserializeConnectedClient(object clientObj)
    {
        var client = new ConnectedClient();
        
        // Handle JsonElement (from JSON deserialization)
        if (clientObj is JsonElement elem)
        {
            if (elem.TryGetProperty("connectionId", out var connectionIdElem))
                client.ConnectionId = connectionIdElem.GetInt64();
            if (elem.TryGetProperty("playerId", out var playerIdElem))
                client.PlayerId = Guid.Parse(playerIdElem.GetString());
            if (elem.TryGetProperty("playerName", out var playerNameElem))
                client.PlayerName = playerNameElem.GetString() ?? string.Empty;
            if (elem.TryGetProperty("isObserver", out var isObserverElem))
                client.IsObserver = isObserverElem.GetBoolean();
        }
        // Handle Dictionary<string, object> (from manual deserialization)
        else if (clientObj is Dictionary<string, object> clientDict)
        {
            if (clientDict.TryGetValue("connectionId", out var connectionIdObj) && connectionIdObj != null)
                client.ConnectionId = Convert.ToInt64(connectionIdObj);
            if (clientDict.TryGetValue("playerId", out var playerIdObj) && playerIdObj != null)
                client.PlayerId = Guid.Parse(playerIdObj.ToString());
            if (clientDict.TryGetValue("playerName", out var playerNameObj) && playerNameObj != null)
                client.PlayerName = playerNameObj.ToString() ?? string.Empty;
            if (clientDict.TryGetValue("isObserver", out var isObserverObj) && isObserverObj != null)
                client.IsObserver = Convert.ToBoolean(isObserverObj);
        }
        
        return client;
    }
    
    /// <summary>
    /// Deserializes a list of connected clients
    /// </summary>
    private List<ConnectedClient> DeserializeConnectedClients(object clientsObj)
    {
        var clients = new List<ConnectedClient>();
        
        // Handle JsonElement (from JSON deserialization)
        if (clientsObj is JsonElement elem)
        {
            foreach (var item in elem.EnumerateArray())
            {
                clients.Add(DeserializeConnectedClient(item));
            }
        }
        // Handle List<object> (from manual deserialization)
        else if (clientsObj is List<object> clientsList)
        {
            foreach (var clientObj in clientsList)
            {
                clients.Add(DeserializeConnectedClient(clientObj));
            }
        }
        
        return clients;
    }
    
    /// <summary>
    /// Deserializes a player object
    /// </summary>
    private Player DeserializePlayer(object playerObj)
    {
        var player = new Player();

        // Handle JsonElement (from JSON deserialization)
        if (playerObj is JsonElement elem)
        {
            if (elem.TryGetProperty("id", out var idElem))
                player.Id = Guid.Parse(idElem.GetString());
            if (elem.TryGetProperty("name", out var nameElem))
                player.Name = nameElem.GetString() ?? string.Empty;
            if (elem.TryGetProperty("isObserver", out var isObserverElem))
                player.IsObserver = isObserverElem.GetBoolean();
            if (elem.TryGetProperty("isConnected", out var isConnectedElem))
                player.IsConnected = isConnectedElem.GetBoolean();
            if (elem.TryGetProperty("index", out var indexElem))
                player.Index = indexElem.GetInt32();
        }
        // Handle Dictionary<string, object> (from manual deserialization)
        else if (playerObj is Dictionary<string, object> playerDict)
        {
            if (playerDict.TryGetValue("Id", out var idObj) && idObj != null)
                player.Id = Guid.Parse(idObj.ToString());
            if (playerDict.TryGetValue("Name", out var nameObj) && nameObj != null)
                player.Name = nameObj.ToString() ?? string.Empty;
            if (playerDict.TryGetValue("IsObserver", out var isObserverObj) && isObserverObj != null)
                player.IsObserver = Convert.ToBoolean(isObserverObj);
            if (playerDict.TryGetValue("IsConnected", out var isConnectedObj) && isConnectedObj != null)
                player.IsConnected = Convert.ToBoolean(isConnectedObj);
            if (playerDict.TryGetValue("Index", out var indexObj) && indexObj != null)
                player.Index = Convert.ToInt32(indexObj);
        }

        return player;
    }

    /// <summary>
    /// Deserializes a list of players
    /// </summary>
    private List<Player> DeserializePlayers(object playersObj)
    {
        var players = new List<Player>();

        // Handle JsonElement (from JSON deserialization)
        if (playersObj is JsonElement elem)
        {
            foreach (var item in elem.EnumerateArray())
            {
                players.Add(DeserializePlayer(item));
            }
        }
        // Handle List<object> (from manual deserialization)
        else if (playersObj is List<object> playersList)
        {
            foreach (var playerObj in playersList)
            {
                players.Add(DeserializePlayer(playerObj));
            }
        }

        return players;
    }

    /// <summary>
    /// Deserializes a game info object
    /// </summary>
    private GameInfo DeserializeGamesInfo(object gamesObj)
    {
        var game = new GameInfo();

        // Handle JsonElement (from JSON deserialization)
        if (gamesObj is JsonElement elem)
        {
            if (elem.TryGetProperty("gameId", out var gameIdElem))
                game.GameId = Guid.Parse(gameIdElem.GetString());
            if (elem.TryGetProperty("gameName", out var gameNameElem))
                game.GameName = gameNameElem.GetString() ?? string.Empty;
            if (elem.TryGetProperty("creatorId", out var creatorIdElem))
                game.CreatorId = Guid.Parse(creatorIdElem.GetString());
            if (elem.TryGetProperty("creatorName", out var creatorNameElem))
                game.CreatorName = creatorNameElem.GetString() ?? string.Empty;
            if (elem.TryGetProperty("playerCount", out var playerCountElem))
                game.PlayerCount = playerCountElem.GetInt32();
            if (elem.TryGetProperty("maxPlayers", out var maxPlayersElem))
                game.MaxPlayers = maxPlayersElem.GetInt32();
            if (elem.TryGetProperty("isAiEnabled", out var isAiEnabledElem))
                game.IsAiEnabled = isAiEnabledElem.GetBoolean();
            if (elem.TryGetProperty("isStarted", out var isStartedElem))
                game.IsStarted = isStartedElem.GetBoolean();
        }
        // Handle Dictionary<string, object> (from manual deserialization)
        else if (gamesObj is Dictionary<string, object> gameDict)
        {
            if (gameDict.TryGetValue("GameId", out var gameIdObj) && gameIdObj != null)
                game.GameId = Guid.Parse(gameIdObj.ToString());
            if (gameDict.TryGetValue("GameName", out var gameNameObj) && gameNameObj != null)
                game.GameName = gameNameObj.ToString() ?? string.Empty;
            if (gameDict.TryGetValue("CreatorId", out var creatorIdObj) && creatorIdObj != null)
                game.CreatorId = Guid.Parse(creatorIdObj.ToString());
            if (gameDict.TryGetValue("CreatorName", out var creatorNameObj) && creatorNameObj != null)
                game.CreatorName = creatorNameObj.ToString() ?? string.Empty;
            if (gameDict.TryGetValue("PlayerCount", out var playerCountObj) && playerCountObj != null)
                game.PlayerCount = Convert.ToInt32(playerCountObj);
            if (gameDict.TryGetValue("MaxPlayers", out var maxPlayersObj) && maxPlayersObj != null)
                game.MaxPlayers = Convert.ToInt32(maxPlayersObj);
            if (gameDict.TryGetValue("IsAiEnabled", out var isAiEnabledObj) && isAiEnabledObj != null)
                game.IsAiEnabled = Convert.ToBoolean(isAiEnabledObj);
            if (gameDict.TryGetValue("IsStarted", out var isStartedObj) && isStartedObj != null)
                game.IsStarted = Convert.ToBoolean(isStartedObj);
        }

        return game;
    }

    /// <summary>
    /// Deserializes a list of games
    /// </summary>
    private List<GameInfo> DeserializeGames(object gamesObj)
    {
        var games = new List<GameInfo>();

        // Handle JsonElement (from JSON deserialization)
        if (gamesObj is JsonElement elem)
        {
            foreach (var item in elem.EnumerateArray())
            {
                games.Add(DeserializeGamesInfo(item));
            }
        }
        // Handle List<object> (from manual deserialization)
        else if (gamesObj is List<object> gamesList)
        {
            foreach (var gameObj in gamesList)
            {
                games.Add(DeserializeGamesInfo(gameObj));
            }
        }

        return games;
    }

    /// <summary>
    /// Deserializes a game state object
    /// </summary>
    private GameState DeserializeGameState(object gameStateObj)
    {
        var gameState = new GameState();

        // Handle JsonElement (from JSON deserialization)
        if (gameStateObj is JsonElement elem)
        {
            if (elem.TryGetProperty("gameId", out var gameIdElem))
                gameState.GameId = Guid.Parse(gameIdElem.GetString());
            if (elem.TryGetProperty("status", out var statusElem))
                gameState.Status = (GameStatus)Enum.Parse(typeof(GameStatus), statusElem.GetString());
            if (elem.TryGetProperty("players", out var playersElem))
            {
                var players = DeserializePlayers(playersElem);
                foreach (var player in players)
                {
                    gameState.Players.Add(player);
                }
            }
            if (elem.TryGetProperty("currentPlayerIndex", out var currentPlayerIndexElem))
                gameState.CurrentPlayerIndex = currentPlayerIndexElem.GetInt32();
            if (elem.TryGetProperty("turnNumber", out var turnNumberElem))
                gameState.TurnNumber = turnNumberElem.GetInt32();
        }
        // Handle Dictionary<string, object> (from manual deserialization)
        else if (gameStateObj is Dictionary<string, object> gameStateDict)
        {
            if (gameStateDict.TryGetValue("GameId", out var gameIdObj) && gameIdObj != null)
                gameState.GameId = Guid.Parse(gameIdObj.ToString());
            if (gameStateDict.TryGetValue("Status", out var statusObj) && statusObj != null)
                gameState.Status = (GameStatus)Enum.Parse(typeof(GameStatus), statusObj.ToString());
            if (gameStateDict.TryGetValue("Players", out var playersObj) && playersObj != null)
            {
                var players = DeserializePlayers(playersObj);
                foreach (var player in players)
                {
                    gameState.Players.Add(player);
                }
            }
            if (gameStateDict.TryGetValue("CurrentPlayerIndex", out var currentPlayerIndexObj) && currentPlayerIndexObj != null)
                gameState.CurrentPlayerIndex = Convert.ToInt32(currentPlayerIndexObj);
            if (gameStateDict.TryGetValue("TurnNumber", out var turnNumberObj) && turnNumberObj != null)
                gameState.TurnNumber = Convert.ToInt32(turnNumberObj);
        }

        return gameState;
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
            Log.Write("Disconnected from server");
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
    public event EventHandler<ConnectedClientsUpdateEventArgs>? OnConnectedClientsUpdate;
    
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
    
    protected virtual void OnConnectedClientsUpdateRaise(List<ConnectedClient> clients)
    {
        OnConnectedClientsUpdate?.Invoke(this, new ConnectedClientsUpdateEventArgs(clients));
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

/// <summary>
/// Event arguments for connected clients update event
/// </summary>
public class ConnectedClientsUpdateEventArgs : EventArgs
{
    public List<ConnectedClient> Clients { get; }
    
    public ConnectedClientsUpdateEventArgs(List<ConnectedClient> clients)
    {
        Clients = clients;
    }
}
