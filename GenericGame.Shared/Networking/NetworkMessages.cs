using GenericGame.Shared.Models;

namespace GenericGame.Shared.Networking;

/// <summary>
/// Message sent when a client connects to the server
/// </summary>
public class ConnectRequestMessage
{
    public string ClientName { get; set; } = string.Empty;
    public bool IsObserver { get; set; }
}

/// <summary>
/// Message sent when a client disconnects
/// </summary>
public class DisconnectRequestMessage
{
    public Guid PlayerId { get; set; }
}

/// <summary>
/// Message containing a player action
/// </summary>
public class PlayerActionMessage
{
    public Guid PlayerId { get; set; }
    public GenericGame.Shared.Models.Action Action { get; set; } = new PlayerAction();
}

/// <summary>
/// Message sent when a player joins the lobby
/// </summary>
public class LobbyJoinMessage
{
    public string PlayerName { get; set; } = string.Empty;
    public bool IsObserver { get; set; }
}

/// <summary>
/// Message sent when a player leaves the lobby
/// </summary>
public class LobbyLeaveMessage
{
    public Guid PlayerId { get; set; }
}

/// <summary>
/// Welcome message sent by server to client
/// </summary>
public class ServerWelcomeMessage
{
    public Guid ServerId { get; set; }
    public Guid PlayerId { get; set; }
    public string ServerMessage { get; set; } = string.Empty;
}

/// <summary>
/// Game state update message
/// </summary>
public class GameUpdateMessage
{
    public GameState GameState { get; set; } = new GameState();
}

/// <summary>
/// Event notification message
/// </summary>
public class EventNotificationMessage
{
    public Event Event { get; set; } = new GameEvent();
}

/// <summary>
/// Message sent when a player joins the game
/// </summary>
public class PlayerJoinedMessage
{
    public Player Player { get; set; } = new Player();
}

/// <summary>
/// Message sent when a player leaves the game
/// </summary>
public class PlayerLeftMessage
{
    public Guid PlayerId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
}

/// <summary>
/// Message sent when a game starts
/// </summary>
public class GameStartedMessage
{
    public Guid GameId { get; set; }
    public GameState GameState { get; set; } = new GameState();
}

/// <summary>
/// Message sent when a game ends
/// </summary>
public class GameEndedMessage
{
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
