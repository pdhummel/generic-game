using GenericGame.Server;
using GenericGame.Shared.Models;
using TicTacToe.Models;
using TicTacToe.AI;

namespace TicTacToe.Game;

/// <summary>
/// Tic-Tac-Toe game implementation with server-side game logic
/// </summary>
public class TicTacToeGame : GameInstance
{
    private readonly TicTacToeAI _ai = new();

    /// <summary>
    /// Creates a new Tic-Tac-Toe game instance
    /// </summary>
    /// <param name="name">Name of the game</param>
    /// <param name="creatorId">ID of the player who created this game</param>
    /// <param name="isAiEnabled">Whether AI opponent is enabled</param>
    /// <param name="isFirstPlayerRandom">Whether first player is randomly chosen</param>
    public TicTacToeGame(string name, Guid creatorId, bool isAiEnabled = false, bool isFirstPlayerRandom = true) : base(name, creatorId)
    {
        State = new TicTacToeGameState();
        State.GameId = GameId;
        State.Status = GameStatus.Lobby;
        State.IsAiEnabled = isAiEnabled;
        State.IsFirstPlayerRandom = isFirstPlayerRandom;

        // Add creator as player 1
        var creator = new Player
        {
            Id = creatorId,
            Name = "Player 1",
            IsObserver = false,
            IsConnected = true
        };
        AddPlayer(creator);

        // If AI is enabled, add AI as player 2
        if (isAiEnabled)
        {
            var aiPlayer = new Player
            {
                Id = Guid.NewGuid(),
                Name = "AI Opponent",
                IsObserver = false,
                IsConnected = true
            };
            AddPlayer(aiPlayer);
        }
    }

    /// <summary>
    /// Gets the current Tic-Tac-Toe game state
    /// </summary>
    public new TicTacToeGameState State { get; protected set; } = new TicTacToeGameState();

    /// <summary>
    /// Processes a player action (placing a mark on the board)
    /// </summary>
    public override void ProcessAction(GenericGame.Shared.Models.Action action, Guid playerId)
    {
        // Validate that the player is in the game
        if (!State.Players.Any(p => p.Id == playerId))
            return;

        // Only allow players (not observers) to make moves
        var player = State.Players.FirstOrDefault(p => p.Id == playerId);
        if (player?.IsObserver == true)
            return;

        // Validate action type
        if (action is not TicTacToeAction tttAction)
            return;

        // Validate game is in progress
        if (State.Status != GameStatus.Playing)
            return;

        // Validate position is empty
        if (State.Board[tttAction.Row, tttAction.Column] != 0)
            return;

        // Validate it's this player's turn
        var currentPlayerIndex = GetPlayerIndex(playerId);
        if (currentPlayerIndex != State.CurrentPlayerIndex)
            return;

        // Place the mark
        var playerMark = currentPlayerIndex == 0 ? 1 : 2; // Player 0 = X (1), Player 1 = O (2)
        State.Board[tttAction.Row, tttAction.Column] = playerMark;

        // Check for win or draw
        CheckWinCondition();

        // Notify all players
        OnActionProcessedRaise(action, playerId);

        // Update turn
        UpdateNextTurn();
    }

    /// <summary>
    /// Updates to the next player's turn
    /// </summary>
    protected override void UpdateNextTurn()
    {
        lock (State)
        {
            State.TurnNumber++;
            State.CurrentPlayerIndex = (State.CurrentPlayerIndex + 1) % 2; // Tic-Tac-Toe is always 2 players

            // Notify of turn change
            OnGameStateUpdatedRaise(State.Clone());
        }
    }

    /// <summary>
    /// Checks for win or draw condition
    /// </summary>
    private void CheckWinCondition()
    {
        // Check rows
        for (int i = 0; i < 3; i++)
        {
            if (State.Board[i, 0] != 0 && State.Board[i, 0] == State.Board[i, 1] && State.Board[i, 0] == State.Board[i, 2])
            {
                State.Winner = State.Board[i, 0];
                EndGame();
                return;
            }
        }

        // Check columns
        for (int j = 0; j < 3; j++)
        {
            if (State.Board[0, j] != 0 && State.Board[0, j] == State.Board[1, j] && State.Board[0, j] == State.Board[2, j])
            {
                State.Winner = State.Board[0, j];
                EndGame();
                return;
            }
        }

        // Check diagonals
        if (State.Board[0, 0] != 0 && State.Board[0, 0] == State.Board[1, 1] && State.Board[0, 0] == State.Board[2, 2])
        {
            State.Winner = State.Board[0, 0];
            EndGame();
            return;
        }

        if (State.Board[0, 2] != 0 && State.Board[0, 2] == State.Board[1, 1] && State.Board[0, 2] == State.Board[2, 0])
        {
            State.Winner = State.Board[0, 2];
            EndGame();
            return;
        }

        // Check for draw (board full)
        bool isFull = true;
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (State.Board[i, j] == 0)
                {
                    isFull = false;
                    break;
                }
            }
        }

        if (isFull)
        {
            State.Winner = 3; // Draw
            EndGame();
        }
    }

    /// <summary>
    /// Gets the player index for a given player ID
    /// </summary>
    private int GetPlayerIndex(Guid playerId)
    {
        var players = State.Players.Where(p => !p.IsObserver).ToList();
        return players.FindIndex(p => p.Id == playerId);
    }

    /// <summary>
    /// Starts the game
    /// </summary>
    public override void StartGame()
    {
        lock (State)
        {
            State.Status = GameStatus.Playing;
            State.TurnNumber = 1;

            // Determine first player
            if (State.IsFirstPlayerRandom)
            {
                State.CurrentPlayerIndex = new Random().Next(0, 2);
            }
            else
            {
                State.CurrentPlayerIndex = 0; // Player 1 starts
            }

            OnGameStartedRaise(State.Clone());
        }
    }

    /// <summary>
    /// Ends the game
    /// </summary>
    public override void EndGame()
    {
        lock (State)
        {
            State.Status = GameStatus.Finished;
            OnGameEndedRaise(State.Clone());
        }
    }

    /// <summary>
    /// Adds a player to the game
    /// </summary>
    public new bool AddPlayer(Player player)
    {
        return base.AddPlayer(player);
    }

    /// <summary>
    /// Gets the number of human players (excluding AI)
    /// </summary>
    public int GetHumanPlayerCount()
    {
        return State.Players.Count(p => !p.IsObserver && p.Name != "AI Opponent");
    }

    /// <summary>
    /// Gets the number of players needed to start the game
    /// </summary>
    public int GetPlayersNeeded()
    {
        return 2; // Tic-Tac-Toe needs 2 players
    }

    /// <summary>
    /// Checks if the game is ready to start
    /// </summary>
    public bool IsReadyToStart()
    {
        return GetHumanPlayerCount() + (State.IsAiEnabled ? 1 : 0) >= GetPlayersNeeded();
    }
}
