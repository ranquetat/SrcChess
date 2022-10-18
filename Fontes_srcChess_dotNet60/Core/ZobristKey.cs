using System;

namespace SrcChess2.Core {
    /// <summary>
    /// Zobrist key implementation.
    /// </summary>
    public static class ZobristKey {

        /// <summary>Random value for each piece/position</summary>
        private static readonly Int64[] s_rndTable;

        /// <summary>
        /// Static constructor. Use to create the random value for each case of the board.
        /// </summary>
        static ZobristKey() {
            Random rnd;
            
            rnd        = new Random(0);
            s_rndTable = new long[64 * 16];
            for (int i = 0; i < 64 * 16; i++) {
                s_rndTable[i] = rnd.NextInt64();
            }
        }

        /// <summary>
        /// Update the Zobrist key using the specified move
        /// </summary>
        /// <param name="zobristKey"> Zobrist key</param>
        /// <param name="pos">        Piece position</param>
        /// <param name="oldPiece">   Old value</param>
        /// <param name="newPiece">   New value</param>
        public static long UpdateZobristKey(long zobristKey, int pos, ChessBoard.PieceType oldPiece, ChessBoard.PieceType newPiece) {
            int baseIndex;
            
            baseIndex   = pos << 4;
            zobristKey ^= s_rndTable[baseIndex + ((int)oldPiece)] ^
                          s_rndTable[baseIndex + ((int)newPiece)];
            return zobristKey; 
        }

        /// <summary>
        /// Update the Zobrist key using the specified move
        /// </summary>
        /// <param name="zobristKey"> Zobrist key</param>
        /// <param name="pos1">       Piece position</param>
        /// <param name="oldPiece1">  Old value</param>
        /// <param name="newPiece1">  New value</param>
        /// <param name="pos2">       Piece position</param>
        /// <param name="oldPiece2">  Old value</param>
        /// <param name="newPiece2">  New value</param>
        public static long UpdateZobristKey(long                 zobristKey,
                                            int                  pos1,
                                            ChessBoard.PieceType oldPiece1,
                                            ChessBoard.PieceType newPiece1,
                                            int                  pos2,
                                            ChessBoard.PieceType oldPiece2,
                                            ChessBoard.PieceType newPiece2) {
            int baseIndex1;
            int baseIndex2;
            
            baseIndex1  = pos1 << 4;
            baseIndex2  = pos2 << 4;
            zobristKey ^= s_rndTable[baseIndex1 + ((int)oldPiece1)] ^
                          s_rndTable[baseIndex1 + ((int)newPiece1)] ^
                          s_rndTable[baseIndex2 + ((int)oldPiece2)] ^
                          s_rndTable[baseIndex2 + ((int)newPiece2)];
            return zobristKey;
        }

        /// <summary>
        /// Compute the zobrist key for a board
        /// </summary>
        /// <param name="board">    Board</param>
        public static long ComputeBoardZobristKey(ChessBoard.PieceType[] board) {
            long retVal = 0;
            
            for (int i = 0; i < 64; i++) {
                retVal ^= s_rndTable[(i << 4) + (int)board[i]];
            }
            return retVal;
        }
    } // Class ZobristKey
} // Namespace
