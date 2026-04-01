using GenericGame.Shared.Models;

namespace TicTacToe.Models;

/// <summary>
/// Tic-Tac-Toe event - mark placed on the board
/// </summary>
public class MarkPlacedEvent : Event
{
    /// <summary>
    /// Row position (0-2)
    /// </summary>
    public int Row { get; set; }

    /// <summary>
    /// Column position (0-2)
    /// </summary>
    public int Column { get; set; }

    /// <summary>
    /// Player who placed the mark (1 = X, 2 = O)
    /// </summary>
    public int Player { get; set; }

    /// <summary>
    /// Creates a deep copy of this event
    /// </summary>
    public override Event Clone()
    {
        return new MarkPlacedEvent
        {
            Timestamp = this.Timestamp,
            Row = this.Row,
            Column = this.Column,
            Player = this.Player
        };
    }
}

/// <summary>
/// Tic-Tac-Toe event - game state changed
/// </summary>
public class GameStateChangedEvent : Event
{
    /// <summary>
    /// Current game state
    /// </summary>
    public TicTacToeGameState GameState { get; set; } = new TicTacToeGameState();

    /// <summary>
    /// Creates a deep copy of this event
    /// </summary>
    public override Event Clone()
    {
        return new GameStateChangedEvent
        {
            Timestamp = this.Timestamp,
            GameState = (TicTacToeGameState)GameState.Clone()
        };
    }
}

/// <summary>
/// Tic-Tac-Toe event - turn changed
/// </summary>
public class TurnChangedEvent : Event
{
    /// <summary>
    /// Player index whose turn it is
    /// </summary>
    public int PlayerIndex { get; set; }

    /// <summary>
    /// Creates a deep copy of this event
    /// </summary>
    public override Event Clone()
    {
        return new TurnChangedEvent
        {
            Timestamp = this.Timestamp,
            PlayerIndex = this.PlayerIndex
        };
    }
}

/// <summary>
/// Tic-Tac-Toe event - game ended
/// </summary>
public class GameEndedEvent : Event
{
    /// <summary>
    /// Winner (0 = none/draw, 1 = X, 2 = O)
    /// </summary>
    public int Winner { get; set; }

    /// <summary>
    /// Creates a deep copy of this event
    /// </summary>
    public override Event Clone()
    {
        return new GameEndedEvent
        {
            Timestamp = this.Timestamp,
            Winner = this.Winner
        };
    }
}
