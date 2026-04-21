using GenericGame.Client;
using GenericGame.Shared.Models;
using GenericGame.Shared.Networking;
using GenericGame;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TicTacToe.Models;

namespace TicTacToe.UI;

/// <summary>
/// Lobby screen that shows connected clients and available games
/// </summary>
public class LobbyScreen : Microsoft.Xna.Framework.Game
{
    private readonly GameClient _client;
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch? _spriteBatch;
    private SpriteFont? _arialFont;
    private Texture2D? _whitePixel;
    private TicTacToeLobbyState _lobbyState;
    private bool _isMyInvitation = false;

    public LobbyScreen(GameClient client)
    {
        _client = client;
        _lobbyState = new TicTacToeLobbyState();

        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = 800,
            PreferredBackBufferHeight = 600
        };

        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        // Register event handlers in constructor so they're registered before any messages are received
        _client.OnGameStateUpdated += OnGameStateUpdated;
        _client.OnLobbyUpdate += OnLobbyUpdate;
        _client.OnGamesListUpdate += OnGamesListUpdate;
        _client.OnGameInvitationReceived += OnGameInvitationReceived;
        _client.OnConnectedClientsUpdate += OnConnectedClientsUpdate;
    }

    protected override void Initialize()
    {
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _arialFont = Content.Load<SpriteFont>("Arial");

        // Create a simple white pixel texture for drawing
        _whitePixel = new Texture2D(GraphicsDevice, 1, 1);
        _whitePixel.SetData(new[] { Color.White });

        base.LoadContent();
    }

    protected override void UnloadContent()
    {
        _whitePixel?.Dispose();
        _whitePixel = null;
        _spriteBatch?.Dispose();
        _spriteBatch = null;

        base.UnloadContent();
    }

    private void OnGameStateUpdated(object sender, GameStateUpdatedEventArgs e)
    {
        // Handle game state updates
    }

    private void OnLobbyUpdate(object sender, LobbyUpdateEventArgs e)
    {
        Log.Write("OnLobbyUpdate(): enter");
        Log.Write($"OnLobbyUpdate: Received {e.Players.Count} players");
        _lobbyState.Players = e.Players;
        _lobbyState.Games = e.Games;
        _lobbyState.CurrentPlayerId = _client.CurrentPlayer?.Id ?? Guid.Empty;
        
        // Debug: Print all players
        foreach (var player in _lobbyState.Players)
        {
            Log.Write($"  - Player: {player.Name} (ID: {player.Id})");
        }
    }
    
    private void OnConnectedClientsUpdate(object sender, ConnectedClientsUpdateEventArgs e)
    {
        Log.Write("OnConnectedClientsUpdate(): enter");
        Log.Write($"OnConnectedClientsUpdate: Received {e.Clients.Count} connected clients");
        _lobbyState.ConnectedClients = e.Clients;
        
        // Debug: Print all connected clients
        foreach (var client in _lobbyState.ConnectedClients)
        {
            Log.Write($"OnConnectedClientsUpdate(): Client: {client.PlayerName} (ID: {client.PlayerId})");
        }
    }
    
    private void OnGamesListUpdate(object sender, GamesListUpdateEventArgs e)
    {
        _lobbyState.Games = e.Games;
    }

    private void OnGameInvitationReceived(object sender, GameInvitationReceivedEventArgs e)
    {
        // Check if this invitation is for me
        if (_client.CurrentPlayer != null && e.InviterId != _client.CurrentPlayer.Id)
        {
            _isMyInvitation = true;
            // Show invitation dialog
            ShowInvitationDialog(e.GameId, e.GameName, e.InviterName);
        }
    }

    private void ShowInvitationDialog(Guid gameId, string gameName, string inviterName)
    {
        // For now, just print to console
        Log.Write($"Invitation received: {inviterName} invited you to {gameName}");
    }

    // Button state tracking
    private bool _mouseLeftButtonPressed = false;
    private bool _createGameButtonClicked = false;

    protected override void Update(GameTime gameTime)
    {
        // Poll for network events from the server
        _client.Update();

        var keyboardState = Keyboard.GetState();
        var mouseState = Mouse.GetState();

        if (keyboardState.IsKeyDown(Keys.Escape))
        {
            Exit();
        }
        else if (keyboardState.IsKeyDown(Keys.C))
        {
            // Create a new game
            var createGameScreen = new CreateGameScreen(_client);
            createGameScreen.Run();
        }

        // Handle mouse click for Create Game button
        const int buttonX = 50;
        const int buttonY = 550;
        const int buttonWidth = 200;
        const int buttonHeight = 40;

        if (mouseState.LeftButton == ButtonState.Pressed && !_mouseLeftButtonPressed)
        {
            if (mouseState.X >= buttonX && mouseState.X < buttonX + buttonWidth &&
                mouseState.Y >= buttonY && mouseState.Y < buttonY + buttonHeight)
            {
                _createGameButtonClicked = true;
            }
        }

        if (_createGameButtonClicked && mouseState.LeftButton == ButtonState.Released)
        {
            _createGameButtonClicked = false;
            _mouseLeftButtonPressed = false;
            var createGameScreen = new CreateGameScreen(_client);
            createGameScreen.Run();
        }

        _mouseLeftButtonPressed = mouseState.LeftButton == ButtonState.Pressed;

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        _spriteBatch?.Begin();

        // Draw title
        DrawText("Tic-Tac-Toe Lobby", 50, 50, Color.White, 24);

        // Draw connected clients section
        DrawText("Connected Clients:", 50, 100, Color.LightGray, 16);
        var yPos = 130f;
        foreach (var client in _lobbyState.ConnectedClients)
        {
            var isMe = client.PlayerId == _lobbyState.CurrentPlayerId ? " (You)" : "";
            DrawText($"- {client.PlayerName} {client.ConnectionId} {client.PlayerId}", 50, yPos, Color.White, 14);
            yPos += 25;
            //Log.Write($"Draw(): {client.PlayerName}{isMe}");
        }

        // Draw games section
        yPos += 30;
        DrawText("Available Games:", 50, yPos, Color.LightGray, 16);
        yPos += 30;
        foreach (var game in _lobbyState.Games)
        {
            var aiText = game.IsAiEnabled ? " (AI)" : "";
            DrawText($"- {game.GameName} (Creator: {game.CreatorName}, Players: {game.PlayerCount}/{game.MaxPlayers}{aiText})", 50, yPos, Color.White, 14);
            yPos += 25;
        }

        if (_lobbyState.Games.Count == 0)
        {
            DrawText("No games available", 50, yPos, Color.Gray, 14);
        }

        // Draw instructions
        yPos += 40;
        DrawText("Controls:", 50, yPos, Color.LightGray, 16);
        yPos += 25;
        DrawText("- C: Create New Game", 50, yPos, Color.Gray, 14);
        yPos += 25;
        DrawText("- Escape: Exit", 50, yPos, Color.Gray, 14);

        // Draw Create Game button
        const int buttonX = 50;
        const int buttonY = 550;
        const int buttonWidth = 200;
        const int buttonHeight = 40;
        var mouseState = Mouse.GetState();
        var isHovering = mouseState.X >= buttonX && mouseState.X < buttonX + buttonWidth &&
                         mouseState.Y >= buttonY && mouseState.Y < buttonY + buttonHeight;

        // Draw button background
        var buttonColor = isHovering ? Color.LightBlue : Color.DodgerBlue;
        _spriteBatch.Draw(_whitePixel!, new Rectangle(buttonX, buttonY, buttonWidth, buttonHeight), buttonColor);

        // Draw button border
        _spriteBatch.Draw(_whitePixel!, new Rectangle(buttonX, buttonY, buttonWidth, 2), Color.White);
        _spriteBatch.Draw(_whitePixel!, new Rectangle(buttonX, buttonY + buttonHeight - 2, buttonWidth, 2), Color.White);
        _spriteBatch.Draw(_whitePixel!, new Rectangle(buttonX, buttonY, 2, buttonHeight), Color.White);
        _spriteBatch.Draw(_whitePixel!, new Rectangle(buttonX + buttonWidth - 2, buttonY, 2, buttonHeight), Color.White);

        // Draw button text
        DrawText("Create New Game", buttonX + buttonWidth / 2 - 60, buttonY + 10, Color.White, 18);

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void DrawText(string text, float x, float y, Color color, float fontSize)
    {
        // Use SpriteFont DrawString for text rendering
        // Scale the font size based on the Arial font (14pt base)
        var font = _arialFont;
        if (font != null)
        {
            var scale = fontSize / 14f;
            var origin = new Vector2(0, 0);
            _spriteBatch?.DrawString(font, text, new Vector2(x, y), color, 0, origin, scale, SpriteEffects.None, 0);
        }
    }
}
