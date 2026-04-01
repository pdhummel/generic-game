# Generic Game Stack

A generic game development framework built with .NET 10, MonoGame, Myra, and LiteNetLib that supports 1-6 players with server-authoritative game state.

## Features

- **Multiplayer Support**: Up to 12 clients can connect (6 players + 6 observers)
- **Server-Authoritative**: Server maintains authoritative game state and rules
- **Action/Event Pattern**: Clients send Actions, Server publishes Events
- **Lobby System**: Players can join a lobby and create games
- **Flexible Game Modes**: Stand-alone server, client-only, or combined server+client
- **Extensible Architecture**: Easy to create new games by extending base classes

## Project Structure

```
generic-game/
├── GenericGame.Shared/          # Shared code between server and client
│   ├── Models/                  # Core models (GameState, Action, Event, Player)
│   ├── Networking/              # Network message types and serialization
│   └── GenericGame.Shared.csproj
├── GenericGame.Server/          # Server-side code
│   ├── Game/                    # Server game logic
│   ├── Program.cs               # Server entry point
│   └── GenericGame.Server.csproj
├── GenericGame.Client/          # Client-side code
│   ├── Game/                    # Client game logic
│   ├── Program.cs               # Client entry point
│   └── GenericGame.Client.csproj
└── TicTacToe/                   # Reference Tic-Tac-Toe implementation
    ├── Models/                  # Tic-Tac-Toe specific models
    ├── Game/                    # Tic-Tac-Toe game logic
    ├── AI/                      # AI opponent implementation
    ├── UI/                      # Client UI
    ├── Program.cs               # Launcher supporting all modes
    └── TicTacToe.csproj
```

## Running the Code

### Stand-alone Server

```bash
# Using dotnet CLI
dotnet run --project GenericGame.Server -- --port 14000

# Or using the Tic-Tac-Toe launcher
dotnet run --project TicTacToe -- --mode server --port 14000
```

### Client Only

```bash
# Connect to a remote server
dotnet run --project GenericGame.Client -- --host 192.168.1.100 --port 14000 --name "Player1"

# Or using the Tic-Tac-Toe launcher
dotnet run --project TicTacToe -- --mode client --host 192.168.1.100 --port 14000 --name "Player1"
```

### Server and Client (Combined)

```bash
# Run both on the same machine
dotnet run --project TicTacToe -- --mode both --port 14000 --name "Player1"
```

## Creating a New Game

To create a new game that extends this framework:

### 1. Create Game-Specific Models

Create models that extend the base classes:

```csharp
// Models/MyGameGameState.cs
using GenericGame.Shared.Models;

namespace MyGame.Models;

public class MyGameGameState : GameState
{
    // Add game-specific state properties
    public int Score { get; set; }
    public string BoardState { get; set; } = string.Empty;
    
    public override GameState Clone()
    {
        var clone = (MyGameGameState)base.Clone();
        clone.Score = Score;
        clone.BoardState = BoardState;
        return clone;
    }
}
```

### 2. Create Action Classes

Create actions representing player choices:

```csharp
// Models/MyGameAction.cs
using GenericGame.Shared.Models;

namespace MyGame.Models;

public class MyGameAction : Action
{
    public int MoveType { get; set; }
    public int Position { get; set; }
    
    public override Action Clone()
    {
        return new MyGameAction
        {
            PlayerId = this.PlayerId,
            Timestamp = this.Timestamp,
            MoveType = this.MoveType,
            Position = this.Position
        };
    }
}
```

### 3. Create Event Classes

Create events for game state changes:

```csharp
// Models/MyGameEvent.cs
using GenericGame.Shared.Models;

namespace MyGame.Models;

public class MoveMadeEvent : Event
{
    public int PlayerIndex { get; set; }
    public int Position { get; set; }
    
    public override Event Clone()
    {
        return new MoveMadeEvent
        {
            Timestamp = this.Timestamp,
            PlayerIndex = this.PlayerIndex,
            Position = this.Position
        };
    }
}
```

### 4. Create Game Instance

Create a game instance that implements game rules:

```csharp
// Game/MyGameInstance.cs
using GenericGame.Server;
using GenericGame.Shared.Models;
using MyGame.Models;

namespace MyGame.Game;

public class MyGameInstance : GameInstance
{
    public new MyGameGameState State => (MyGameGameState)base.State;
    
    public MyGameInstance(string name, Guid creatorId) : base(name, creatorId)
    {
        State = new MyGameGameState();
        State.GameId = GameId;
        State.Status = GameStatus.Lobby;
    }
    
    public override void ProcessAction(Action action, Guid playerId)
    {
        // Validate action
        if (action is not MyGameAction myAction) return;
        
        // Validate game state
        if (State.Status != GameStatus.Playing) return;
        
        // Apply game rules
        // ... your game logic here ...
        
        // Update state
        State.Score += 10;
        
        // Notify players
        OnActionProcessed?.Invoke(this, new ActionProcessedEventArgs(action, playerId));
        OnGameStateUpdated?.Invoke(this, new GameStateUpdatedEventArgs(State.Clone()));
    }
}
```

### 5. Create Client UI

Create a client UI that handles events:

```csharp
// UI/MyGameForm.cs
using System.Windows.Forms;
using GenericGame.Client;
using MyGame.Models;

namespace MyGame.UI;

public class MyGameForm : Form
{
    private readonly GameClient _client;
    private readonly MyGameGameState _gameState = new MyGameGameState();
    
    public MyGameForm(GameClient client)
    {
        _client = client;
        _client.OnGameStateUpdated += OnGameStateUpdated;
        _client.OnEventReceived += OnEventReceived;
        
        InitializeComponent();
    }
    
    private void OnGameStateUpdated(object sender, GameStateUpdatedEventArgs e)
    {
        _gameState = (MyGameGameState)e.GameState;
        UpdateUI();
    }
    
    private void OnEventReceived(object sender, EventReceivedEventArgs e)
    {
        if (e.Event is MoveMadeEvent moveEvent)
        {
            // Handle move event
        }
    }
    
    private void UpdateUI()
    {
        // Update UI based on game state
    }
}
```

### 6. Create Launcher

Create a launcher that supports all modes:

```csharp
// Program.cs
using GenericGame.Client;
using GenericGame.Server;
using MyGame.Game;
using MyGame.UI;

namespace MyGame;

class Program
{
    static void Main(string[] args)
    {
        string mode = "client";
        string serverAddress = "localhost";
        int serverPort = 14000;
        string playerName = "Player";
        
        // Parse command line arguments...
        
        switch (mode)
        {
            case "server":
                RunServerOnly(serverPort);
                break;
            case "client":
                RunClientOnly(serverAddress, serverPort, playerName);
                break;
            case "both":
                RunServerAndClient(serverPort, playerName);
                break;
        }
    }
    
    static void RunClientOnly(string address, int port, string playerName)
    {
        var client = new GameClient();
        client.Connect(address, port, playerName, false);
        Application.Run(new MyGameForm(client));
        client.Disconnect();
    }
    
    static void RunServerAndClient(int port, string playerName)
    {
        var server = new GameServer(port);
        var serverThread = new Thread(() => server.Run()) { IsBackground = true };
        serverThread.Start();
        Thread.Sleep(1000);
        
        var client = new GameClient();
        client.Connect("localhost", port, playerName, false);
        Application.Run(new MyGameForm(client));
        client.Disconnect();
    }
}
```

## Game State Visibility

Game state can be separated as:

- **Shared**: Visible to all players (e.g., board state)
- **Per-Player**: Unique to each player (e.g., hand cards)
- **Hidden**: Only visible to specific players (e.g., opponent's cards)

The framework provides the base `GameState` class. Implement visibility logic in your game-specific code by:

1. Marking properties with visibility attributes
2. Filtering state before sending to clients
3. Using separate state objects for different visibility levels

## Network Protocol

The framework uses LiteNetLib for networking with the following message types:

- `ConnectRequest`: Client connects to server
- `DisconnectRequest`: Client disconnects
- `PlayerAction`: Client sends action to server
- `LobbyJoin`: Client joins lobby
- `LobbyLeave`: Client leaves lobby
- `ServerWelcome`: Server welcomes client
- `GameUpdate`: Server sends game state update
- `EventNotification`: Server sends event notification
- `PlayerJoined`: Server notifies of new player
- `PlayerLeft`: Server notifies of player leaving
- `GameStarted`: Server notifies game has started
- `GameEnded`: Server notifies game has ended

## License

MIT License