using System.Windows.Controls;
using SrcChess2.Core;

namespace SrcChess2 {
    /// <summary>
    /// Defines a set of chess pieces. A piece set is a set of xaml which defines the representation of each pieces
    /// </summary>
    public abstract class PieceSet {

        /// <summary>
        /// List of standard pieces
        /// </summary>
        protected enum ChessPiece {
            /// <summary>No Piece</summary>
            None         = -1,
            /// <summary>Black Pawn</summary>
            Black_Pawn   = 0,
            /// <summary>Black Rook</summary>
            Black_Rook   = 1,
            /// <summary>Black Bishop</summary>
            Black_Bishop = 2,
            /// <summary>Black Knight</summary>
            Black_Knight = 3,
            /// <summary>Black Queen</summary>
            Black_Queen  = 4,
            /// <summary>Black King</summary>
            Black_King   = 5,
            /// <summary>White Pawn</summary>
            White_Pawn   = 6,
            /// <summary>White Rook</summary>
            White_Rook   = 7,
            /// <summary>White Bishop</summary>
            White_Bishop = 8,
            /// <summary>White Knight</summary>
            White_Knight = 9,
            /// <summary>White Queen</summary>
            White_Queen  = 10,
            /// <summary>White King</summary>
            White_King   = 11
        };

        
        /// <summary>Name of the piece set</summary>
        public string Name { get; private set; }

        /// <summary>
        /// Class Ctor
        /// </summary>
        /// <param name="name">  Piece set Name</param>
        protected PieceSet(string name) => Name = name;

        /// <summary>
        /// Transform a ChessBoard piece into a ChessPiece enum
        /// </summary>
        /// <param name="pieceType"></param>
        /// <returns></returns>
        private static ChessPiece GetChessPieceFromPiece(ChessBoard.PieceType pieceType)
            => pieceType switch {
                ChessBoard.PieceType.Pawn   | ChessBoard.PieceType.White => ChessPiece.White_Pawn,
                ChessBoard.PieceType.Knight | ChessBoard.PieceType.White => ChessPiece.White_Knight,
                ChessBoard.PieceType.Bishop | ChessBoard.PieceType.White => ChessPiece.White_Bishop,
                ChessBoard.PieceType.Rook   | ChessBoard.PieceType.White => ChessPiece.White_Rook,
                ChessBoard.PieceType.Queen  | ChessBoard.PieceType.White => ChessPiece.White_Queen,
                ChessBoard.PieceType.King   | ChessBoard.PieceType.White => ChessPiece.White_King,
                ChessBoard.PieceType.Pawn   | ChessBoard.PieceType.Black => ChessPiece.Black_Pawn,
                ChessBoard.PieceType.Knight | ChessBoard.PieceType.Black => ChessPiece.Black_Knight,
                ChessBoard.PieceType.Bishop | ChessBoard.PieceType.Black => ChessPiece.Black_Bishop,
                ChessBoard.PieceType.Rook   | ChessBoard.PieceType.Black => ChessPiece.Black_Rook,
                ChessBoard.PieceType.Queen  | ChessBoard.PieceType.Black => ChessPiece.Black_Queen,
                ChessBoard.PieceType.King   | ChessBoard.PieceType.Black => ChessPiece.Black_King,
                _                                                        => ChessPiece.None,
            };

        /// <summary>
        /// Load a new piece
        /// </summary>
        /// <param name="chessPiece">   Chess Piece</param>
        protected abstract UserControl LoadPiece(ChessPiece chessPiece);

        /// <summary>
        /// Gets the specified piece
        /// </summary>
        /// <param name="pieceType"> Piece type</param>
        /// <returns>
        /// User control expressing the piece
        /// </returns>
        public UserControl? this[ChessBoard.PieceType pieceType] {
            get {
                UserControl? retVal;
                ChessPiece   chessPiece;

                chessPiece  = GetChessPieceFromPiece(pieceType);
                retVal      = chessPiece == ChessPiece.None ? null : LoadPiece(chessPiece);
                return retVal;
            }
        }
    } // Class PieceSet
} // Namespace
