using GenericGame.Client;
using GenericGame.Shared.Models;
using GenericGame.Shared.Networking;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TicTacToe.Models;

namespace TicTacToe.UI;

/// <summary>
/// Screen for creating a new Tic-Tac-Toe game
/// </summary>
public class CreateGameScreen : Microsoft.Xna.Framework.Game
{
    private readonly GameClient _client;
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch? _spriteBatch;
    private Texture2D? _whitePixel;
    private TicTacToeLobbyState _lobbyState;

    // Game creation options
    private string _gameName = "New Game";
    private bool _isAiEnabled = false;
    private bool _isFirstPlayerRandom = true;
    private string _firstPlayerChoice = "Random"; // "Random", "X", "O"
    private List<Guid> _invitedPlayerIds = new List<Guid>();
    private int _selectedPlayerIndex = -1;

    // Font constants
    private const float CharWidth = 8;
    private const float CharHeight = 12;
    private const float CharSpacing = 2;

    public CreateGameScreen(GameClient client)
    {
        _client = client;
        _lobbyState = new TicTacToeLobbyState();

        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = 600,
            PreferredBackBufferHeight = 500
        };

        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        // Register event handlers
        _client.OnLobbyUpdate += OnLobbyUpdate;
        _client.OnGamesListUpdate += OnGamesListUpdate;

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

    protected override void Update(GameTime gameTime)
    {
        var keyboardState = Keyboard.GetState();
        if (keyboardState.IsKeyDown(Keys.Escape))
        {
            Exit();
            return;
        }

        // Handle keyboard navigation
        if (keyboardState.IsKeyDown(Keys.Down))
        {
            _selectedPlayerIndex = (_selectedPlayerIndex + 1) % _lobbyState.Players.Count;
        }
        else if (keyboardState.IsKeyDown(Keys.Up))
        {
            _selectedPlayerIndex = (_selectedPlayerIndex - 1 + _lobbyState.Players.Count) % _lobbyState.Players.Count;
        }
        else if (keyboardState.IsKeyDown(Keys.Space))
        {
            // Toggle player selection
            if (_selectedPlayerIndex >= 0 && _selectedPlayerIndex < _lobbyState.Players.Count)
            {
                var player = _lobbyState.Players[_selectedPlayerIndex];
                if (player.Id != _client.CurrentPlayer?.Id)
                {
                    if (_invitedPlayerIds.Contains(player.Id))
                    {
                        _invitedPlayerIds.Remove(player.Id);
                    }
                    else
                    {
                        _invitedPlayerIds.Add(player.Id);
                    }
                }
            }
        }
        else if (keyboardState.IsKeyDown(Keys.A))
        {
            // Toggle AI opponent
            _isAiEnabled = !_isAiEnabled;
        }
        else if (keyboardState.IsKeyDown(Keys.R))
        {
            // Toggle random first player
            _isFirstPlayerRandom = !_isFirstPlayerRandom;
            if (_isFirstPlayerRandom)
            {
                _firstPlayerChoice = "Random";
            }
        }
        else if (keyboardState.IsKeyDown(Keys.Tab))
        {
            // Cycle through first player choices
            _firstPlayerChoice = _firstPlayerChoice switch
            {
                "Random" => "X",
                "X" => "O",
                "O" => "Random",
                _ => "Random"
            };
            _isFirstPlayerRandom = _firstPlayerChoice == "Random";
        }
        else if (keyboardState.IsKeyDown(Keys.Enter))
        {
            // Create the game
            CreateGame();
        }

        base.Update(gameTime);
    }

    private void CreateGame()
    {
        _client.CreateGame(_gameName, _invitedPlayerIds, _isAiEnabled, _isFirstPlayerRandom);
        Exit();
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        _spriteBatch?.Begin();

        // Draw title
        DrawText("Create New Game", 50, 50, Color.White, 24);

        // Draw game name input
        DrawText("Game Name:", 50, 100, Color.LightGray, 16);
        DrawText(_gameName, 200, 100, Color.White, 16);

        // Draw players section
        DrawText("Invite Players (Space to select, Enter to create):", 50, 150, Color.LightGray, 16);

        var yPos = 180f;
        foreach (var player in _lobbyState.Players)
        {
            var isSelected = player.Id == _lobbyState.Players[_selectedPlayerIndex].Id;
            var isInvited = _invitedPlayerIds.Contains(player.Id);
            var isMe = player.Id == _lobbyState.CurrentPlayerId;

            var prefix = isSelected ? "> " : "  ";
            var invitedText = isInvited ? " [INVITED]" : "";
            var meText = isMe ? " (You)" : "";

            DrawText($"{prefix}{player.Name}{meText}{invitedText}", 50, yPos, isSelected ? Color.Yellow : Color.White, 14);
            yPos += 25;
        }

        // Draw options
        yPos += 30;
        DrawText("Options:", 50, yPos, Color.LightGray, 16);
        yPos += 30;

        // AI Opponent option
        var aiText = _isAiEnabled ? "Yes" : "No";
        DrawText($"AI Opponent: {aiText}", 50, yPos, Color.White, 14);
        DrawCheckbox(400, yPos, _isAiEnabled);
        yPos += 25;

        // First Player option
        DrawText($"First Player: {_firstPlayerChoice}", 50, yPos, Color.White, 14);
        DrawCheckbox(400, yPos, !_isFirstPlayerRandom);
        yPos += 25;

        // Draw instructions
        yPos += 30;
        DrawText("Controls:", 50, yPos, Color.LightGray, 16);
        yPos += 25;
        DrawText("- Up/Down: Navigate players", 50, yPos, Color.Gray, 14);
        yPos += 25;
        DrawText("- Space: Toggle player selection", 50, yPos, Color.Gray, 14);
        yPos += 25;
        DrawText("- A: Toggle AI opponent", 50, yPos, Color.Gray, 14);
        yPos += 25;
        DrawText("- Tab: Cycle first player (Random/X/O)", 50, yPos, Color.Gray, 14);
        yPos += 25;
        DrawText("- Enter: Create game", 50, yPos, Color.Gray, 14);
        yPos += 25;
        DrawText("- Escape: Back to lobby", 50, yPos, Color.Gray, 14);

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void DrawCheckbox(float x, float y, bool isChecked)
    {
        const int boxSize = 16;
        const int boxSpacing = 20;

        // Draw checkbox border
        _spriteBatch.Draw(_whitePixel!, new Rectangle((int)x, (int)y, boxSize, boxSize), isChecked ? Color.Lime : Color.Gray);

        // Draw checkmark if checked
        if (isChecked)
        {
            // Simple checkmark pattern
            var checkColor = Color.Black;
            // Draw diagonal line 1
            for (int i = 3; i <= 12; i++)
            {
                _spriteBatch.Draw(_whitePixel!, new Rectangle((int)x + 3 + i, (int)y + 12 - i, 2, 2), checkColor);
            }
            // Draw diagonal line 2
            for (int i = 0; i <= 8; i++)
            {
                _spriteBatch.Draw(_whitePixel!, new Rectangle((int)x + 3 + i, (int)y + 4 + i, 2, 2), checkColor);
            }
        }
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
