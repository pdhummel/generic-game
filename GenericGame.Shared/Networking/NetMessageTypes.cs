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
    public const byte CreateGame = 6;
    public const byte InvitePlayer = 7;
    public const byte JoinGame = 8;
    public const byte LeaveGame = 9;

    // Server to Client messages
    public const byte ServerWelcome = 10;
    public const byte GameUpdate = 11;
    public const byte EventNotification = 12;
    public const byte PlayerJoined = 13;
    public const byte PlayerLeft = 14;
    public const byte GameStarted = 15;
    public const byte GameEnded = 16;
    public const byte LobbyUpdate = 17;
    public const byte GamesListUpdate = 18;
    public const byte GameInvitation = 19;
    public const byte PlayerInvited = 20;
    public const byte PlayersListUpdate = 21;
    public const byte ConnectedClientsUpdate = 22;
    public const byte CreateGameResponse = 23;
    public const byte JoinGameResponse = 23;
    public const byte LeaveGameResponse = 24;
    public const byte LobbyJoinResponse = 25;
    public const byte LobbyLeaveResponse = 26;
}
