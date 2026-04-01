using GenericGame.Shared.Models;

namespace TicTacToe.Models;

/// <summary>
/// Tic-Tac-Toe player action - placing a mark on the board
/// </summary>
public class TicTacToeAction : GenericGame.Shared.Models.Action
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
    /// Creates a deep copy of this action
    /// </summary>
    public override GenericGame.Shared.Models.Action Clone()
    {
        return new TicTacToeAction
        {
            PlayerId = this.PlayerId,
            Timestamp = this.Timestamp,
            Row = this.Row,
            Column = this.Column
        };
    }
}
