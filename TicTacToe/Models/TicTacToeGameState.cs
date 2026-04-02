using GenericGame.Shared.Models;

namespace TicTacToe.Models;

/// <summary>
/// Tic-Tac-Toe specific game state
/// </summary>
public class TicTacToeGameState : GameState
{
    /// <summary>
    /// 3x3 game board (0 = empty, 1 = X, 2 = O)
    /// </summary>
    public int[,] Board { get; set; } = new int[3, 3];

    /// <summary>
    /// Winner of the game (0 = none, 1 = X, 2 = O, 3 = draw)
    /// </summary>
    public int Winner { get; set; } = 0;

    /// <summary>
    /// Whether AI opponent is enabled
    /// </summary>
    public bool IsAiEnabled { get; set; }

    /// <summary>
    /// Whether first player is randomly chosen
    /// </summary>
    public bool IsFirstPlayerRandom { get; set; }

    /// <summary>
    /// Creates a deep copy of this game state
    /// </summary>
    public override GameState Clone()
    {
        var clone = (TicTacToeGameState)base.Clone();
        Array.Copy(Board, clone.Board, 9);
        clone.Winner = Winner;
        clone.IsAiEnabled = IsAiEnabled;
        clone.IsFirstPlayerRandom = IsFirstPlayerRandom;
        return clone;
    }
}
