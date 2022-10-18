
using GenericSearchEngine;

namespace SrcChess2.Core {

    /// <summary>Implements a board evaluation methods</summary>
    /// Board:  63 62 61 60 59 58 57 56
    ///         55 54 53 52 51 50 49 48
    ///         47 46 45 44 43 42 41 40
    ///         39 38 37 36 35 34 33 32
    ///         31 30 29 28 27 26 25 24
    ///         23 22 21 20 19 18 17 16
    ///         15 14 13 12 11 10 9  8
    ///         7  6  5  4  3  2  1  0
    /// Each position contains a PieceE enum with PieceE.White or PieceE.Black
    /// 
    /// m_piPiecesCount[PieceE.Pawn | PieceE.White .. PieceE.King | PieceE.White] for white
    /// m_piPiecesCount[PieceE.Pawn | PieceE.Black .. PieceE.King | PieceE.Black] for black
    /// Black and White king position are set using the board position.
    /// 
    public interface IBoardEvaluation {

        /// <summary>
        /// Name of the board evaluation method.
        /// </summary>
        string  Name { get; }

        /// <summary>
        /// Evaluates a board. The number of point is greater than 0 if white is in advantage, less than 0 if black is.
        /// </summary>
        /// <param name="board">          Board</param>
        /// <param name="piecesCount">    Number of each pieces</param>
        /// <param name="attackPosInfo">  Information about pieces position</param>
        /// <param name="whiteKingPos">   Position of the white king</param>
        /// <param name="blackKingPos">   Position of the black king</param>
        /// <param name="whiteCastle">    White has castled</param>
        /// <param name="blackCastle">    Black has castled</param>
        /// <param name="moveCountDelta"> Number of possible white moves - Number of possible black moves</param>
        /// <returns>
        /// Points
        /// </returns>
        int Points(ChessBoard.PieceType[] board,int[] piecesCount, AttackPosInfo attackPosInfo, int whiteKingPos, int blackKingPos, bool whiteCastle, bool blackCastle, int moveCountDelta);
    }
}
