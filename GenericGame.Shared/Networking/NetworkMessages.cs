using GenericGame.Shared.Models;

namespace GenericGame.Shared.Networking;

/// <summary>
/// Message sent when a client connects to the server
/// </summary>
public class ConnectRequestMessage
{
    public byte MessageType { get; set; } = NetMessageType.ConnectRequest;
    public string ClientName { get; set; } = string.Empty;
    public bool IsObserver { get; set; }
}

/// <summary>
/// Message sent when a client disconnects
/// </summary>
public class DisconnectRequestMessage
{
    public byte MessageType { get; set; } = NetMessageType.DisconnectRequest;
    public Guid PlayerId { get; set; }
}

/// <summary>
/// Message containing a player action
/// </summary>
public class PlayerActionMessage
{
    public byte MessageType { get; set; } = NetMessageType.PlayerAction;
    public Guid PlayerId { get; set; }
    public GenericGame.Shared.Models.Action Action { get; set; } = new PlayerAction();
}

/// <summary>
/// Message sent when a player joins the lobby
/// </summary>
public class LobbyJoinMessage
{
    public byte MessageType { get; set; } = NetMessageType.LobbyJoin;
    public string PlayerName { get; set; } = string.Empty;
    public bool IsObserver { get; set; }
}

/// <summary>
/// Message sent when a player leaves the lobby
/// </summary>
public class LobbyLeaveMessage
{
    public byte MessageType { get; set; } = NetMessageType.LobbyLeave;
    public Guid PlayerId { get; set; }
}

/// <summary>
/// Welcome message sent by server to client
/// </summary>
public class ServerWelcomeMessage
{
    public byte MessageType { get; set; } = NetMessageType.ServerWelcome;
    public Guid ServerId { get; set; }
    public Guid PlayerId { get; set; }
    public string ServerMessage { get; set; } = string.Empty;
}

/// <summary>
/// Game state update message
/// </summary>
public class GameUpdateMessage
{
    public byte MessageType { get; set; } = NetMessageType.GameUpdate;
    public GameState GameState { get; set; } = new GameState();
}

/// <summary>
/// Event notification message
/// </summary>
public class EventNotificationMessage
{
    public byte MessageType { get; set; } = NetMessageType.EventNotification;
    public Event Event { get; set; } = new GameEvent();
}

/// <summary>
/// Message sent when a player joins the game
/// </summary>
public class PlayerJoinedMessage
{
    public byte MessageType { get; set; } = NetMessageType.PlayerJoined;
    public Player Player { get; set; } = new Player();
}

/// <summary>
/// Message sent when a player leaves the game
/// </summary>
public class PlayerLeftMessage
{
    public byte MessageType { get; set; } = NetMessageType.PlayerLeft;
    public Guid PlayerId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
}

/// <summary>
/// Message sent when a game starts
/// </summary>
public class GameStartedMessage
{
    public byte MessageType { get; set; } = NetMessageType.GameStarted;
    public Guid GameId { get; set; }
    public GameState GameState { get; set; } = new GameState();
}

/// <summary>
/// Message sent when a game ends
/// </summary>
public class GameEndedMessage
{
    public byte MessageType { get; set; } = NetMessageType.GameEnded;
    public Guid GameId { get; set; }
    public GameState GameState { get; set; } = new GameState();
}

/// <summary>
/// Generic player action (base class for actions)
/// </summary>
public class PlayerAction : GenericGame.Shared.Models.Action
{
    public override GenericGame.Shared.Models.Action Clone()
    {
        return new PlayerAction();
    }
}

/// <summary>
/// Generic game event (base class for events)
/// </summary>
public class GameEvent : GenericGame.Shared.Models.Event
{
    public override GenericGame.Shared.Models.Event Clone()
    {
        return new GameEvent();
    }
}

/// <summary>
/// Message sent when a player creates a new game
/// </summary>
public class CreateGameMessage
{
    public byte MessageType { get; set; } = NetMessageType.CreateGame;
    public string GameName { get; set; } = string.Empty;
    public Guid CreatorId { get; set; }
    public List<Guid> InvitedPlayerIds { get; set; } = new List<Guid>();
    public bool IsAiEnabled { get; set; }
    public bool IsFirstPlayerRandom { get; set; }
}

/// <summary>
/// Message sent when a player invites another player to join a game
/// </summary>
public class InvitePlayerMessage
{
    public byte MessageType { get; set; } = NetMessageType.InvitePlayer;
    public Guid GameId { get; set; }
    public Guid InviterId { get; set; }
    public Guid InviteeId { get; set; }
}

/// <summary>
/// Message sent when a player accepts an invitation and joins a game
/// </summary>
public class JoinGameMessage
{
    public byte MessageType { get; set; } = NetMessageType.JoinGame;
    public Guid GameId { get; set; }
    public Guid PlayerId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
}

/// <summary>
/// Message sent when a player leaves a game
/// </summary>
public class LeaveGameMessage
{
    public byte MessageType { get; set; } = NetMessageType.LeaveGame;
    public Guid GameId { get; set; }
    public Guid PlayerId { get; set; }
}

/// <summary>
/// Message containing the list of available games
/// </summary>
public class GamesListMessage
{
    public byte MessageType { get; set; } = NetMessageType.GamesListUpdate;
    public List<GameInfo> Games { get; set; } = new List<GameInfo>();
}

/// <summary>
/// Information about a game
/// </summary>
public class GameInfo
{
    public Guid GameId { get; set; }
    public string GameName { get; set; } = string.Empty;
    public Guid CreatorId { get; set; }
    public string CreatorName { get; set; } = string.Empty;
    public int PlayerCount { get; set; }
    public int MaxPlayers { get; set; }
    public bool IsAiEnabled { get; set; }
    public bool IsStarted { get; set; }
}

/// <summary>
/// Message sent when a player is invited to join a game
/// </summary>
public class GameInvitationMessage
{
    public byte MessageType { get; set; } = NetMessageType.GameInvitation;
    public Guid GameId { get; set; }
    public string GameName { get; set; } = string.Empty;
    public Guid InviterId { get; set; }
    public string InviterName { get; set; } = string.Empty;
}

/// <summary>
/// Message sent when a player accepts an invitation
/// </summary>
public class InvitationAcceptedMessage
{
    public byte MessageType { get; set; } = NetMessageType.JoinGame;
    public Guid GameId { get; set; }
    public Guid PlayerId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
}

/// <summary>
/// Message sent when a game is ready to start
/// </summary>
public class GameReadyToStartMessage
{
    public byte MessageType { get; set; } = NetMessageType.GameStarted;
    public Guid GameId { get; set; }
    public GameState GameState { get; set; } = new GameState();
}

/// <summary>
/// Message containing the list of connected players
/// </summary>
public class PlayersListMessage
{
    public byte MessageType { get; set; } = NetMessageType.PlayersListUpdate;
    public List<Player> Players { get; set; } = new List<Player>();
}

/// <summary>
/// Message containing the list of connected clients (not necessarily players)
/// </summary>
public class ConnectedClientsMessage
{
    public byte MessageType { get; set; } = NetMessageType.ConnectedClientsUpdate;
    public List<ConnectedClient> Clients { get; set; } = new List<ConnectedClient>();
}

/// <summary>
/// Represents a connected client (may or may not be a player)
/// </summary>
public class ConnectedClient
{
    public long ConnectionId { get; set; }
    public Guid PlayerId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public bool IsObserver { get; set; }
}

/// <summary>
/// Response message for create game
/// </summary>
public class CreateGameResponseMessage
{
    public byte MessageType { get; set; } = NetMessageType.CreateGameResponse;
    public bool Success { get; set; }
    public Guid GameId { get; set; }
    public GameState GameState { get; set; } = new GameState();
}
