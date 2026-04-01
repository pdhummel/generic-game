using GenericGame.Client;
using GenericGame.Shared.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TicTacToe.Models;

namespace TicTacToe.UI;

/// <summary>
/// Tic-Tac-Toe client UI using MonoGame
/// </summary>
public class TicTacToeForm : Microsoft.Xna.Framework.Game
{
    private readonly GameClient _client;
    private TicTacToeGameState _gameState;
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch? _spriteBatch;
    private Texture2D? _whitePixel;
    private bool _isMyTurn = false;

    public TicTacToeForm(GameClient client)
    {
        _client = client;
        _gameState = new TicTacToeGameState();
        
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = 400,
            PreferredBackBufferHeight = 450
        };

        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        _client.OnGameStateUpdated += OnGameStateUpdated;
        _client.OnGameStarted += OnGameStarted;
        _client.OnGameEnded += OnGameEnded;

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        
        // Create a simple white pixel texture for drawing grid lines
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

    private void OnGameStateUpdated(object sender, GenericGame.Client.GameStateUpdatedEventArgs e)
    {
        _gameState = (TicTacToeGameState)e.GameState;
        UpdateBoard();
    }

    private void OnGameStarted(object sender, GenericGame.Client.GameStartedEventArgs e)
    {
        _gameState = (TicTacToeGameState)e.GameState;
        _isMyTurn = IsMyTurn();
        UpdateBoard();
    }

    private void OnGameEnded(object sender, GenericGame.Client.GameEndedEventArgs e)
    {
        _gameState = (TicTacToeGameState)e.GameState;
        _isMyTurn = false;
        UpdateBoard();
    }

    protected override void Update(GameTime gameTime)
    {
        var keyboardState = Keyboard.GetState();
        if (keyboardState.IsKeyDown(Keys.Escape))
        {
            Exit();
        }

        // Handle mouse clicks for board
        var mouseState = Mouse.GetState();
        if (mouseState.LeftButton == ButtonState.Pressed && _isMyTurn && _gameState.Status == GameStatus.Playing)
        {
            HandleMouseClick(mouseState);
        }

        base.Update(gameTime);
    }

    private void HandleMouseClick(MouseState mouseState)
    {
        // Check if click is on the board
        const int boardX = 50;
        const int boardY = 100;
        const int cellSize = 90;
        const int spacing = 10;

        if (mouseState.X >= boardX && mouseState.X < boardX + 3 * cellSize + 2 * spacing &&
            mouseState.Y >= boardY && mouseState.Y < boardY + 3 * cellSize + 2 * spacing)
        {
            int col = (mouseState.X - boardX) / (cellSize + spacing);
            int row = (mouseState.Y - boardY) / (cellSize + spacing);

            if (col >= 0 && col < 3 && row >= 0 && row < 3)
            {
                // Check if cell is empty
                if (_gameState.Board[row, col] == 0)
                {
                    // Send action to server
                    var action = new TicTacToeAction
                    {
                        Row = row,
                        Column = col
                    };
                    _client.SendAction(action);
                }
            }
        }
    }

    private void UpdateBoard()
    {
        // Update status
        switch (_gameState.Status)
        {
            case GameStatus.Lobby:
                break;
            case GameStatus.Playing:
                var currentPlayer = _gameState.Players[_gameState.CurrentPlayerIndex];
                _isMyTurn = IsMyTurn();
                break;
            case GameStatus.Finished:
                break;
        }
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        _spriteBatch?.Begin();

        // Draw status
        var yPos = 50f;
        switch (_gameState.Status)
        {
            case GameStatus.Lobby:
                DrawText("Waiting for players...", 50, yPos);
                break;
            case GameStatus.Playing:
                var currentPlayer = _gameState.Players[_gameState.CurrentPlayerIndex];
                DrawText($"Turn: {currentPlayer.Name}", 50, yPos);
                if (_isMyTurn)
                {
                    DrawText("Your turn!", 50, yPos + 30);
                }
                break;
            case GameStatus.Finished:
                if (_gameState.Winner == 0)
                    DrawText("Game Over - Draw!", 50, yPos);
                else if (_gameState.Winner == 1)
                    DrawText("Game Over - X Wins!", 50, yPos);
                else
                    DrawText("Game Over - O Wins!", 50, yPos);
                break;
        }

        // Draw board
        const int boardX = 50;
        const int boardY = 150;
        const int cellSize = 90;
        const int spacing = 10;

        // Draw grid lines
        for (int i = 0; i <= 3; i++)
        {
            // Vertical lines
            _spriteBatch.Draw(_whitePixel!, new Rectangle(boardX + i * (cellSize + spacing), boardY, 2, 3 * cellSize + 2 * spacing), Color.White);
            // Horizontal lines
            _spriteBatch.Draw(_whitePixel!, new Rectangle(boardX, boardY + i * (cellSize + spacing), 3 * cellSize + 2 * spacing, 2), Color.White);
        }

        // Draw X and O
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                var mark = _gameState.Board[row, col];
                if (mark != 0)
                {
                    var x = boardX + col * (cellSize + spacing) + cellSize / 2;
                    var y = boardY + row * (cellSize + spacing) + cellSize / 2;
                    var text = mark == 1 ? "X" : "O";
                    DrawText(text, x - 5, y - 7); // Center the text approximately
                }
            }
        }

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void DrawText(string text, float x, float y)
    {
        // Simple text drawing using white pixel texture
        // Each character is drawn as a series of lines
        const float charWidth = 8;
        const float charHeight = 12;
        const float spacing = 2;

        foreach (char c in text)
        {
            DrawCharacter(c, x, y);
            x += charWidth + spacing;
        }
    }

    private void DrawCharacter(char c, float x, float y)
    {
        // Simple 5x7 pixel font representation
        // Each bit represents a pixel (1 = draw, 0 = empty)
        byte[] pattern = GetCharacterPattern(c);

        for (int row = 0; row < 7; row++)
        {
            for (int col = 0; col < 5; col++)
            {
                if ((pattern[row] & (1 << (4 - col))) != 0)
                {
                    _spriteBatch.Draw(_whitePixel!, new Rectangle((int)x + col, (int)y + row, 1, 1), Color.White);
                }
            }
        }
    }

    private byte[] GetCharacterPattern(char c)
    {
        // Simple 5x7 pixel font patterns
        // Each byte represents a row (bits 0-4 are the character pixels)
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

    private bool IsMyTurn()
    {
        // Check if current player is me
        if (_client.CurrentPlayer == null) return false;
        return _gameState.Players[_gameState.CurrentPlayerIndex].Id == _client.CurrentPlayer.Id;
    }
}
