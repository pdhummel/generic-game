using GenericGame.Client;
using GenericGame.Shared.Models;
using GenericGame.Shared.Networking;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TicTacToe.Models;

namespace TicTacToe.UI;

/// <summary>
/// Lobby screen that shows connected players and available games
/// </summary>
public class LobbyScreen : Microsoft.Xna.Framework.Game
{
    private readonly GameClient _client;
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch? _spriteBatch;
    private Texture2D? _whitePixel;
    private TicTacToeLobbyState _lobbyState;
    private bool _isMyInvitation = false;

    // Font constants
    private const float CharWidth = 8;
    private const float CharHeight = 12;
    private const float CharSpacing = 2;

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
    }

    protected override void Initialize()
    {
        // Register event handlers
        _client.OnGameStateUpdated += OnGameStateUpdated;
        _client.OnLobbyUpdate += OnLobbyUpdate;
        _client.OnGamesListUpdate += OnGamesListUpdate;
        _client.OnGameInvitationReceived += OnGameInvitationReceived;

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // Create a simple white pixel texture for drawing
        _whitePixel = new Texture2D(GraphicsDevice, 1, 1);
        _whitePixel.SetData(new[] { Color.White });

        base.LoadContent();
    }

    protected override void UnloadContent()
    {
        _spriteBatch?.Dispose();
        _spriteBatch = null;
        _whitePixel?.Dispose();
        _whitePixel = null;

        base.UnloadContent();
    }

    private void OnGameStateUpdated(object sender, GameStateUpdatedEventArgs e)
    {
        // Handle game state updates
    }

    private void OnLobbyUpdate(object sender, LobbyUpdateEventArgs e)
    {
        _lobbyState.Players = e.Players;
        _lobbyState.Games = e.Games;
        _lobbyState.CurrentPlayerId = _client.CurrentPlayer?.Id ?? Guid.Empty;
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
        Console.WriteLine($"Invitation received: {inviterName} invited you to {gameName}");
    }

    // Button state tracking
    private bool _mouseLeftButtonPressed = false;
    private bool _createGameButtonClicked = false;

    protected override void Update(GameTime gameTime)
    {
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

        // Draw players section
        DrawText("Connected Players:", 50, 100, Color.LightGray, 16);
        var yPos = 130f;
        foreach (var player in _lobbyState.Players)
        {
            var isMe = player.Id == _lobbyState.CurrentPlayerId ? " (You)" : "";
            DrawText($"- {player.Name}{isMe}", 50, yPos, Color.White, 14);
            yPos += 25;
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
        // Simple text drawing using white pixel texture
        foreach (char c in text)
        {
            DrawCharacter(c, x, y, color, fontSize);
            x += (CharWidth + CharSpacing) * (fontSize / 12);
        }
    }

    private void DrawCharacter(char c, float x, float y, Color color, float fontSize)
    {
        // Simple 5x7 pixel font representation
        byte[] pattern = GetCharacterPattern(c);

        float width = (CharWidth + CharSpacing) * (fontSize / 12);
        float height = CharHeight * (fontSize / 12);

        for (int row = 0; row < 7; row++)
        {
            for (int col = 0; col < 5; col++)
            {
                if ((pattern[row] & (1 << (4 - col))) != 0)
                {
                    _spriteBatch?.Draw(_whitePixel!, new Rectangle((int)x + (int)(col * width / 5), (int)y + (int)(row * height / 7), (int)width / 5, (int)height / 7), color);
                }
            }
        }
    }

    private byte[] GetCharacterPattern(char c)
    {
        // Simple 5x7 pixel font patterns
        return c switch
        {
            ' ' => new byte[] { 0, 0, 0, 0, 0, 0, 0 },
            '!' => new byte[] { 4, 4, 4, 4, 0, 4, 0 },
            '"' => new byte[] { 14, 14, 14, 0, 0, 0, 0 },
            '#' => new byte[] { 10, 21, 31, 21, 31, 21, 10 },
            '$' => new byte[] { 4, 13, 15, 4, 13, 5, 12 },
            '%' => new byte[] { 19, 9, 2, 4, 8, 17, 19 },
            '&' => new byte[] { 6, 13, 13, 6, 13, 13, 7 },
            '\'' => new byte[] { 4, 4, 4, 0, 0, 0, 0 },
            '(' => new byte[] { 4, 8, 8, 8, 8, 8, 4 },
            ')' => new byte[] { 4, 2, 2, 2, 2, 2, 4 },
            '*' => new byte[] { 17, 10, 4, 31, 4, 10, 17 },
            '+' => new byte[] { 4, 4, 31, 4, 4, 0, 0 },
            ',' => new byte[] { 4, 4, 4, 0, 0, 0, 0 },
            '-' => new byte[] { 4, 4, 4, 4, 4, 4, 4 },
            '.' => new byte[] { 4, 4, 4, 0, 0, 0, 0 },
            '/' => new byte[] { 2, 4, 8, 16, 8, 4, 2 },
            '0' => new byte[] { 14, 17, 19, 21, 25, 17, 14 },
            '1' => new byte[] { 4, 12, 4, 4, 4, 4, 14 },
            '2' => new byte[] { 14, 17, 1, 2, 4, 8, 31 },
            '3' => new byte[] { 14, 17, 1, 6, 1, 17, 14 },
            '4' => new byte[] { 2, 6, 10, 18, 31, 2, 2 },
            '5' => new byte[] { 31, 16, 16, 30, 1, 17, 14 },
            '6' => new byte[] { 14, 16, 16, 30, 17, 17, 14 },
            '7' => new byte[] { 31, 1, 2, 4, 8, 8, 8 },
            '8' => new byte[] { 14, 17, 17, 14, 17, 17, 14 },
            '9' => new byte[] { 14, 17, 17, 15, 1, 2, 4 },
            ':' => new byte[] { 0, 4, 4, 0, 4, 4, 0 },
            ';' => new byte[] { 4, 4, 4, 0, 4, 4, 0 },
            '<' => new byte[] { 2, 4, 8, 16, 8, 4, 2 },
            '=' => new byte[] { 4, 4, 31, 4, 4, 0, 0 },
            '>' => new byte[] { 8, 4, 2, 1, 2, 4, 8 },
            '?' => new byte[] { 14, 17, 1, 2, 4, 0, 4 },
            '@' => new byte[] { 14, 17, 21, 21, 21, 17, 14 },
            'A' => new byte[] { 4, 10, 17, 31, 17, 17, 17 },
            'B' => new byte[] { 30, 17, 17, 30, 17, 17, 30 },
            'C' => new byte[] { 14, 16, 16, 16, 16, 16, 14 },
            'D' => new byte[] { 28, 18, 17, 17, 17, 18, 28 },
            'E' => new byte[] { 31, 16, 16, 30, 16, 16, 31 },
            'F' => new byte[] { 31, 16, 16, 30, 16, 16, 16 },
            'G' => new byte[] { 14, 16, 16, 17, 19, 17, 14 },
            'H' => new byte[] { 17, 17, 17, 31, 17, 17, 17 },
            'I' => new byte[] { 14, 4, 4, 4, 4, 4, 14 },
            'J' => new byte[] { 15, 4, 4, 4, 4, 16, 8 },
            'K' => new byte[] { 17, 18, 20, 24, 20, 18, 17 },
            'L' => new byte[] { 16, 16, 16, 16, 16, 16, 31 },
            'M' => new byte[] { 17, 27, 21, 17, 17, 17, 17 },
            'N' => new byte[] { 17, 25, 21, 19, 17, 17, 17 },
            'O' => new byte[] { 14, 17, 17, 17, 17, 17, 14 },
            'P' => new byte[] { 30, 17, 17, 30, 16, 16, 16 },
            'Q' => new byte[] { 14, 17, 17, 17, 21, 18, 13 },
            'R' => new byte[] { 30, 17, 17, 30, 20, 18, 17 },
            'S' => new byte[] { 14, 17, 16, 14, 1, 17, 14 },
            'T' => new byte[] { 31, 4, 4, 4, 4, 4, 4 },
            'U' => new byte[] { 17, 17, 17, 17, 17, 17, 14 },
            'V' => new byte[] { 17, 17, 17, 17, 17, 10, 4 },
            'W' => new byte[] { 17, 17, 17, 21, 21, 27, 17 },
            'X' => new byte[] { 17, 17, 10, 4, 10, 17, 17 },
            'Y' => new byte[] { 17, 17, 17, 10, 4, 4, 4 },
            'Z' => new byte[] { 31, 1, 2, 4, 8, 16, 31 },
            '[' => new byte[] { 14, 8, 8, 8, 8, 8, 14 },
            '\\' => new byte[] { 8, 8, 4, 2, 1, 1, 1 },
            ']' => new byte[] { 14, 2, 2, 2, 2, 2, 14 },
            '^' => new byte[] { 4, 10, 17, 0, 0, 0, 0 },
            '_' => new byte[] { 0, 0, 0, 0, 0, 0, 31 },
            '`' => new byte[] { 4, 2, 1, 0, 0, 0, 0 },
            'a' => new byte[] { 0, 0, 6, 1, 15, 17, 15 },
            'b' => new byte[] { 16, 16, 24, 18, 17, 18, 24 },
            'c' => new byte[] { 0, 0, 14, 16, 16, 16, 14 },
            'd' => new byte[] { 1, 1, 15, 17, 17, 19, 14 },
            'e' => new byte[] { 0, 0, 14, 17, 31, 16, 14 },
            'f' => new byte[] { 6, 8, 8, 14, 8, 8, 8 },
            'g' => new byte[] { 0, 0, 15, 17, 15, 1, 14 },
            'h' => new byte[] { 16, 16, 24, 20, 18, 17, 17 },
            'i' => new byte[] { 4, 0, 4, 4, 4, 4, 4 },
            'j' => new byte[] { 2, 0, 2, 2, 2, 2, 11 },
            'k' => new byte[] { 16, 16, 18, 20, 24, 20, 18 },
            'l' => new byte[] { 4, 4, 4, 4, 4, 4, 4 },
            'm' => new byte[] { 0, 0, 22, 21, 21, 21, 21 },
            'n' => new byte[] { 0, 0, 22, 21, 21, 21, 21 },
            'o' => new byte[] { 0, 0, 14, 17, 17, 17, 14 },
            'p' => new byte[] { 0, 0, 30, 17, 30, 16, 16 },
            'q' => new byte[] { 0, 0, 15, 17, 15, 1, 1 },
            'r' => new byte[] { 0, 0, 22, 20, 18, 17, 17 },
            's' => new byte[] { 0, 0, 14, 16, 14, 1, 12 },
            't' => new byte[] { 4, 4, 14, 4, 4, 4, 7 },
            'u' => new byte[] { 0, 0, 17, 17, 17, 19, 14 },
            'v' => new byte[] { 0, 0, 17, 17, 17, 10, 4 },
            'w' => new byte[] { 0, 0, 17, 21, 21, 21, 17 },
            'x' => new byte[] { 0, 0, 17, 10, 4, 10, 17 },
            'y' => new byte[] { 0, 0, 17, 17, 15, 1, 14 },
            'z' => new byte[] { 0, 0, 31, 2, 4, 8, 31 },
            '{' => new byte[] { 4, 8, 8, 14, 8, 8, 4 },
            '|' => new byte[] { 4, 4, 4, 4, 4, 4, 4 },
            '}' => new byte[] { 4, 2, 2, 14, 2, 2, 4 },
            '~' => new byte[] { 6, 9, 0, 0, 0, 0, 0 },
            _ => new byte[] { 0, 0, 0, 0, 0, 0, 0 }
        };
    }
}
