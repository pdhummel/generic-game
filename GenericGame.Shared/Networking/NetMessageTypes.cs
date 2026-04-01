namespace GenericGame.Shared.Networking;

/// <summary>
/// Network message type identifiers
/// </summary>
public static class NetMessageType
{
    // Client to Server messages
    public const byte ConnectRequest = 1;
    public const byte DisconnectRequest = 2;
    public const byte PlayerAction = 3;
    public const byte LobbyJoin = 4;
    public const byte LobbyLeave = 5;

    // Server to Client messages
    public const byte ServerWelcome = 10;
    public const byte GameUpdate = 11;
    public const byte EventNotification = 12;
    public const byte PlayerJoined = 13;
    public const byte PlayerLeft = 14;
    public const byte GameStarted = 15;
    public const byte GameEnded = 16;
}
