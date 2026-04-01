using TicTacToe.Models;

namespace TicTacToe.AI;

/// <summary>
/// Tic-Tac-Toe AI opponent using minimax algorithm
/// </summary>
public class TicTacToeAI
{
    private const int AI_MARK = 2; // O
    private const int HUMAN_MARK = 1; // X

    /// <summary>
    /// Gets the best move for the AI
    /// </summary>
    /// <param name="board">Current game board</param>
    /// <returns>The best move as (row, column), or (-1, -1) if no valid moves</returns>
    public (int Row, int Column) GetBestMove(int[,] board)
    {
        int bestScore = int.MinValue;
        (int Row, int Column) bestMove = (-1, -1);

        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (board[i, j] == 0)
                {
                    board[i, j] = AI_MARK;
                    int score = Minimax(board, 0, false);
                    board[i, j] = 0;

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestMove = (i, j);
                    }
                }
            }
        }

        return bestMove;
    }

    /// <summary>
    /// Minimax algorithm to find the best move
    /// </summary>
    private int Minimax(int[,] board, int depth, bool isMaximizing)
    {
        int winner = CheckWinner(board);
        if (winner == AI_MARK) return 10 - depth;
        if (winner == HUMAN_MARK) return depth - 10;
        if (IsBoardFull(board)) return 0;

        if (isMaximizing)
        {
            int bestScore = int.MinValue;
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (board[i, j] == 0)
                    {
                        board[i, j] = AI_MARK;
                        int score = Minimax(board, depth + 1, false);
                        board[i, j] = 0;
                        bestScore = Math.Max(bestScore, score);
                    }
                }
            }
            return bestScore;
        }
        else
        {
            int bestScore = int.MaxValue;
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (board[i, j] == 0)
                    {
                        board[i, j] = HUMAN_MARK;
                        int score = Minimax(board, depth + 1, true);
                        board[i, j] = 0;
                        bestScore = Math.Min(bestScore, score);
                    }
                }
            }
            return bestScore;
        }
    }

    /// <summary>
    /// Checks if the board is full
    /// </summary>
    private bool IsBoardFull(int[,] board)
    {
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (board[i, j] == 0) return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Checks for a winner
    /// </summary>
    private int CheckWinner(int[,] board)
    {
        // Check rows
        for (int i = 0; i < 3; i++)
        {
            if (board[i, 0] != 0 && board[i, 0] == board[i, 1] && board[i, 0] == board[i, 2])
                return board[i, 0];
        }

        // Check columns
        for (int j = 0; j < 3; j++)
        {
            if (board[0, j] != 0 && board[0, j] == board[1, j] && board[0, j] == board[2, j])
                return board[0, j];
        }

        // Check diagonals
        if (board[0, 0] != 0 && board[0, 0] == board[1, 1] && board[0, 0] == board[2, 2])
            return board[0, 0];

        if (board[0, 2] != 0 && board[0, 2] == board[1, 1] && board[0, 2] == board[2, 0])
            return board[0, 2];

        return 0;
    }
}
