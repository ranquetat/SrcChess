using System.Windows;
using SrcChess2.Core;

namespace SrcChess2 {
    /// <summary>
    /// Ask user for the to pawn promotion piece
    /// </summary>
    public partial class FrmQueryPawnPromotionType : Window {
        /// <summary>Pawn Promotion Piece</summary>
        private readonly ChessBoard.ValidPawnPromotion  m_validPawnPromotion;

        /// <summary>
        /// Class Ctor
        /// </summary>
        public FrmQueryPawnPromotionType() {
            InitializeComponent();
        }

        /// <summary>
        /// Class constructor
        /// </summary>
        /// <param name="validPawnPromotion">  The valid pawn promotion type</param>
        public FrmQueryPawnPromotionType(ChessBoard.ValidPawnPromotion validPawnPromotion) : this() {
            m_validPawnPromotion        = validPawnPromotion;
            radioButtonQueen.IsEnabled  = ((m_validPawnPromotion & ChessBoard.ValidPawnPromotion.Queen)  != ChessBoard.ValidPawnPromotion.None);
            radioButtonRook.IsEnabled   = ((m_validPawnPromotion & ChessBoard.ValidPawnPromotion.Rook)   != ChessBoard.ValidPawnPromotion.None);
            radioButtonBishop.IsEnabled = ((m_validPawnPromotion & ChessBoard.ValidPawnPromotion.Bishop) != ChessBoard.ValidPawnPromotion.None);
            radioButtonKnight.IsEnabled = ((m_validPawnPromotion & ChessBoard.ValidPawnPromotion.Knight) != ChessBoard.ValidPawnPromotion.None);
            radioButtonPawn.IsEnabled   = ((m_validPawnPromotion & ChessBoard.ValidPawnPromotion.Pawn)   != ChessBoard.ValidPawnPromotion.None);
            if ((m_validPawnPromotion & ChessBoard.ValidPawnPromotion.Queen)  != ChessBoard.ValidPawnPromotion.None) {
                radioButtonQueen.IsChecked  = true;
            } else if ((m_validPawnPromotion & ChessBoard.ValidPawnPromotion.Rook)   != ChessBoard.ValidPawnPromotion.None) {
                radioButtonRook.IsChecked   = true;
            } else if ((m_validPawnPromotion & ChessBoard.ValidPawnPromotion.Bishop) != ChessBoard.ValidPawnPromotion.None) {
                radioButtonBishop.IsChecked = true;
            } else if ((m_validPawnPromotion & ChessBoard.ValidPawnPromotion.Knight) != ChessBoard.ValidPawnPromotion.None) {
                radioButtonKnight.IsChecked = true;
            } else if ((m_validPawnPromotion & ChessBoard.ValidPawnPromotion.Pawn)   != ChessBoard.ValidPawnPromotion.None) {
                radioButtonPawn.IsChecked   = true;
            }
        }

        /// <summary>
        /// Get the pawn promotion type
        /// </summary>
        public Move.MoveType PromotionType {
            get {
                Move.MoveType retVal;
                
                if (radioButtonRook.IsChecked == true) {
                    retVal = Move.MoveType.PawnPromotionToRook;
                } else if (radioButtonBishop.IsChecked == true) {
                    retVal = Move.MoveType.PawnPromotionToBishop;
                } else if (radioButtonKnight.IsChecked == true) {
                    retVal = Move.MoveType.PawnPromotionToKnight;
                } else if (radioButtonPawn.IsChecked == true) {
                    retVal = Move.MoveType.PawnPromotionToPawn;
                } else {
                    retVal = Move.MoveType.PawnPromotionToQueen;
                }
                return retVal;
            }
        }

        /// <summary>
        /// Called when the Ok button is clicked
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event Parameter</param>
        private void ButOk_Click(object sender, RoutedEventArgs e) {
            DialogResult = true;
            Close();
        }
    } // Class FrmQueryPawnPromotionType
} // Namespace
