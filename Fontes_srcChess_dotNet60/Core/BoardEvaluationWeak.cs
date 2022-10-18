using GenericSearchEngine;

namespace SrcChess2.Core {
    /// <summary>
    /// Board evaluation function used for beginner
    /// </summary>
    public class BoardEvaluationWeak : IBoardEvaluation {
        /// <summary>Value of each piece/color.</summary>
        static protected int[]      PointPerPiece { get; }

        /// <summary>
        /// Static constructor
        /// </summary>
        static BoardEvaluationWeak() {
            PointPerPiece                                   = new int[16];
            PointPerPiece[(int)ChessBoard.PieceType.Pawn]   = 100;
            PointPerPiece[(int)ChessBoard.PieceType.Rook]   = 100;
            PointPerPiece[(int)ChessBoard.PieceType.Knight] = 100;
            PointPerPiece[(int)ChessBoard.PieceType.Bishop] = 100;
            PointPerPiece[(int)ChessBoard.PieceType.Queen]  = 100;
            PointPerPiece[(int)ChessBoard.PieceType.King]   = 1000000;
            PointPerPiece[(int)(ChessBoard.PieceType.Pawn   | ChessBoard.PieceType.Black)] = -100;
            PointPerPiece[(int)(ChessBoard.PieceType.Rook   | ChessBoard.PieceType.Black)] = -100;
            PointPerPiece[(int)(ChessBoard.PieceType.Knight | ChessBoard.PieceType.Black)] = -100;
            PointPerPiece[(int)(ChessBoard.PieceType.Bishop | ChessBoard.PieceType.Black)] = -100;
            PointPerPiece[(int)(ChessBoard.PieceType.Queen  | ChessBoard.PieceType.Black)] = -100;
            PointPerPiece[(int)(ChessBoard.PieceType.King   | ChessBoard.PieceType.Black)] = -1000000;
        }

        /// <summary>
        /// Name of the evaluation method
        /// </summary>
        public virtual string Name => "Beginner";

        /// <summary>
        /// Evaluates a board. The number of point is greater than 0 if white is in advantage, less than 0 if black is.
        /// </summary>
        /// <param name="board">            Board.</param>
        /// <param name="piecesCount">      Number of each pieces</param>
        /// <param name="attackPosInfo">    Information about pieces position</param>
        /// <param name="whiteKingPos">     Position of the white king</param>
        /// <param name="blackKingPos">     Position of the black king</param>
        /// <param name="whiteCastle">      White has castled</param>
        /// <param name="blackCastle">      Black has castled</param>
        /// <param name="moveCountDelta">   Number of possible white move - Number of possible black move</param>
        /// <returns>
        /// Points
        /// </returns>
        public virtual int Points(ChessBoard.PieceType[] board,
                                  int[]                  piecesCount,
                                  AttackPosInfo          attackPosInfo,
                                  int                    whiteKingPos,
                                  int                    blackKingPos,
                                  bool                   whiteCastle,
                                  bool                   blackCastle,
                                  int                    moveCountDelta) {
            int retVal = 0;
            
            for (int i = 0; i < piecesCount.Length; i++) {
                retVal += PointPerPiece[i] * piecesCount[i];
            }
            return retVal;
        }
    } // Class BoardEvaluationWeak
} // Namespace
