using GenericSearchEngine;

namespace SrcChess2.Core {

    /// <summary>
    /// Basic board evaluation function:
    ///     - Board pieces value for each opponent
    ///     
    /// </summary>
    internal sealed class BoardEvaluationBasic : IBoardEvaluation {

        /// <summary>Value of each piece/color.</summary>
        static private int[] PiecesPoint { get; }

        /// <summary>
        /// Static constructor
        /// </summary>
        static BoardEvaluationBasic() {
            PiecesPoint                                                                  = new int[16];
            PiecesPoint[(int)ChessBoard.PieceType.Pawn]                                  = 100;
            PiecesPoint[(int)ChessBoard.PieceType.Rook]                                  = 500;
            PiecesPoint[(int)ChessBoard.PieceType.Knight]                                = 300;
            PiecesPoint[(int)ChessBoard.PieceType.Bishop]                                = 325;
            PiecesPoint[(int)ChessBoard.PieceType.Queen]                                 = 900;
            PiecesPoint[(int)ChessBoard.PieceType.King]                                  = 1000000;
            PiecesPoint[(int)(ChessBoard.PieceType.Pawn   | ChessBoard.PieceType.Black)] = -100;
            PiecesPoint[(int)(ChessBoard.PieceType.Rook   | ChessBoard.PieceType.Black)] = -500;
            PiecesPoint[(int)(ChessBoard.PieceType.Knight | ChessBoard.PieceType.Black)] = -300;
            PiecesPoint[(int)(ChessBoard.PieceType.Bishop | ChessBoard.PieceType.Black)] = -325;
            PiecesPoint[(int)(ChessBoard.PieceType.Queen  | ChessBoard.PieceType.Black)] = -900;
            PiecesPoint[(int)(ChessBoard.PieceType.King   | ChessBoard.PieceType.Black)] = -1000000;
        }

        /// <summary>
        /// Name of the evaluation method
        /// </summary>
        public string Name => "Basic";

        /// <summary>
        /// Evaluates a board. The number of point is greater than 0 if white is in advantage, less than 0 if black is.
        /// </summary>
        /// <param name="board">            Board</param>
        /// <param name="countPerPiece">    Number of each pieces</param>
        /// <param name="attackPosInfo">    Information about attacking position</param>
        /// <param name="whiteKingPos">     Position of the white king</param>
        /// <param name="blackKingPos">     Position of the black king</param>
        /// <param name="whiteCastle">      White has castled</param>
        /// <param name="blackCastle">      Black has castled</param>
        /// <param name="moveCountDelta">   Number of possible white move - Number of possible black move</param>
        /// <returns>
        /// Points. > 0: White advantage, < 0: Black advantage
        /// </returns>
        public int Points(ChessBoard.PieceType[] board,
                          int[]                  countPerPiece,
                          AttackPosInfo          attackPosInfo,
                          int                    whiteKingPos,
                          int                    blackKingPos,
                          bool                   whiteCastle,
                          bool                   blackCastle,
                          int                    moveCountDelta) {
            int retVal = 0;
            
            for (int i = 0; i < countPerPiece.Length; i++) {
                retVal += PiecesPoint[i] * countPerPiece[i];
            }
            if (board[12] == ChessBoard.PieceType.Pawn) {
                retVal -= 4; // Favor moving king's pawn by 1 or 2
            }
            if (board[52] == (ChessBoard.PieceType.Pawn | ChessBoard.PieceType.Black)) {
                retVal += 4; // Favor moving king's pawn by 1 or 2
            }
            if (whiteCastle) {
                retVal += 10;
            }
            if (blackCastle) {
                retVal -= 10;
            }
            retVal += moveCountDelta;                    // Favor a board with more moves
            retVal += attackPosInfo.PiecesAttacked << 1; // Favor more pieces attacking position
            return retVal;
        }
    } // Class BoardEvaluationBasic
} // Namespace
