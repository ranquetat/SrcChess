using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SrcChess2.Core;

namespace SrcChess2 {
    /// <summary>
    /// Show a list of lost pieces
    /// </summary>
    public partial class LostPiecesControl : UserControl {
        /// <summary>Array of frame containing the piece visual</summary>
        private readonly Border[]               m_borders;
        /// <summary>Array containining the pieces</summary>
        private readonly ChessBoard.PieceType[] m_pieceTypes;
        /// <summary>Chess Board Control</summary>
        private ChessBoardControl?              m_chessBoardCtl;
        /// <summary>Piece Set to use to show the pieces</summary>
        private PieceSet?                       m_pieceSet;
        /// <summary>true if in design mode. In design mode, One of each possible pieces is shown and one can be selected.</summary>
        private bool                            m_isDesignMode;
        /// <summary>Piece currently selected in design mode.</summary>
        private int                             m_selectedPiece;
        /// <summary>Color being displayed. false = White, true = Black</summary>
        public bool                             Color { get; set; }
        
        /// <summary>
        /// Class Ctor
        /// </summary>
        public LostPiecesControl() {
            Border   border;

            InitializeComponent();
            m_selectedPiece    = -1;
            m_borders         = new Border[16];
            m_pieceTypes          = new ChessBoard.PieceType[16];
            for (int i = 0; i < 16; i++) {
                border = new Border {
                    Margin          = new Thickness(1),
                    BorderThickness = new Thickness(1),
                    BorderBrush     = Background
                };
                m_borders[i]       = border;
                m_pieceTypes[i]    = ChessBoard.PieceType.None;
                CellContainer.Children.Add(border);
            }
        }

        /// <summary>
        /// Enumerate the pieces which must be shown in the control
        /// </summary>
        /// <returns>
        /// Array of pieces
        /// </returns>
        private ChessBoard.PieceType[] EnumPiece() {
            ChessBoard.PieceType[] pieceTypes;
            ChessBoard.PieceType[] possiblePieceTypes;
            ChessBoard.PieceType   pieceType;
            int                    eatedPieces;
            int                    pos;
            
            pieceTypes           = new ChessBoard.PieceType[16];
            for (int i = 0; i < 16; i++) {
                pieceTypes[i]   = ChessBoard.PieceType.None;
            }
            possiblePieceTypes  = new ChessBoard.PieceType[] { ChessBoard.PieceType.King,
                                                               ChessBoard.PieceType.Queen,
                                                               ChessBoard.PieceType.Rook,
                                                               ChessBoard.PieceType.Bishop,
                                                               ChessBoard.PieceType.Knight,
                                                               ChessBoard.PieceType.Pawn };
            pos = 0;
            if (m_isDesignMode) {
                pos++;
            }
            foreach (ChessBoard.PieceType possiblePieceType in possiblePieceTypes) {
                if (m_isDesignMode) {
                    pieceType         = possiblePieceType;
                    pieceTypes[pos++] = pieceType;
                    pieceType        |= ChessBoard.PieceType.Black;
                    pieceTypes[pos++] = pieceType;
                } else {                    
                    pieceType = possiblePieceType;
                    if (Color) {
                        pieceType |= ChessBoard.PieceType.Black;
                    }
                    eatedPieces = m_chessBoardCtl!.ChessBoard.GetEatedPieceCount(pieceType);
                    for (int i = 0; i < eatedPieces; i++) {
                        pieceTypes[pos++] = pieceType;
                    }
                }
            }
            return pieceTypes;
        }

        /// <summary>
        /// Make the grid square
        /// </summary>
        /// <param name="size"> User control size</param>
        private static Size MakeSquare(Size size) {
            double  minSize;

            minSize = (size.Width < size.Height) ? size.Width : size.Height;
            size    = new Size(minSize, minSize);
            return size;
        }
        
        /// <summary>
        /// Called when the Measure() method is called
        /// </summary>
        /// <param name="constraint">   Size constraint</param>
        /// <returns>
        /// Control size
        /// </returns>
        protected override Size MeasureOverride(Size constraint) {
            constraint = MakeSquare(constraint);
 	        return base.MeasureOverride(constraint);
        }

        /// <summary>
        /// Set the chess piece control
        /// </summary>
        /// <param name="pos">          Piece position</param>
        /// <param name="pieceType">    Piece type</param>
        private void SetPieceControl(int pos, ChessBoard.PieceType pieceType) {
            Border   border;
            Control? controlPiece;
            Label    label;

            border       = m_borders[pos];
            controlPiece = m_pieceSet![pieceType];
            if (controlPiece != null) {
                controlPiece.Margin = new Thickness(1);
            }
            m_pieceTypes[pos] = pieceType;
            if (controlPiece == null) { // && m_bDesignMode) {
                label = new Label {
                    Content     = " ",
                    FontSize    = 0.1
                };
                controlPiece                     = label;
                controlPiece.HorizontalAlignment = HorizontalAlignment.Stretch;
                controlPiece.VerticalAlignment   = VerticalAlignment.Stretch;
            }
            border.Child = controlPiece;
        }

        /// <summary>
        /// Refresh the specified cell
        /// </summary>
        /// <param name="newPieces">     New pieces value</param>
        /// <param name="pos">           Piece position</param>
        /// <param name="isFullRefresh"> true to refresh even if its the same piece</param>
        private void RefreshCell(ChessBoard.PieceType[] newPieces, int pos, bool isFullRefresh) {
            ChessBoard.PieceType pieceType;

            pieceType = newPieces[pos];
            if (pieceType != m_pieceTypes[pos] || isFullRefresh) {
                SetPieceControl(pos, pieceType);
            }
        }

        /// <summary>
        /// Refresh the board
        /// </summary>
        /// <param name="isFullRefresh">    Refresh even if its the same piece</param>
        private void Refresh(bool isFullRefresh) {
            ChessBoard             chessBoard;
            ChessBoard.PieceType[] newPieceTypes;

            if (m_chessBoardCtl != null && m_chessBoardCtl.ChessBoard != null && m_pieceSet != null) {
                newPieceTypes = EnumPiece();
                chessBoard    = m_chessBoardCtl.ChessBoard;
                if (chessBoard != null) {
                    for (int pos = 0; pos < 16; pos++) {
                        RefreshCell(newPieceTypes, pos, isFullRefresh);
                    }
                }
            }
        }

        /// <summary>
        /// Refresh the board
        /// </summary>
        public void Refresh() => Refresh(isFullRefresh: false);
        
        /// <summary>
        /// Chess Board Control associate with this control
        /// </summary>
        public ChessBoardControl? ChessBoardControl {
            get => m_chessBoardCtl;
            set {
                if (m_chessBoardCtl != value) {
                    m_chessBoardCtl = value;
                    Refresh(isFullRefresh: false);
                }
            }
        }

        /// <summary>
        /// Piece Set use to draw the visual pieces
        /// </summary>
        public PieceSet? PieceSet {
            get => m_pieceSet;
            set {
                if (m_pieceSet != value) {
                    m_pieceSet = value;
                    Refresh(isFullRefresh: true);
                }
            }
        }

        /// <summary>
        /// Select a piece (in design mode only)
        /// </summary>
        public int SelectedIndex {
            get => m_selectedPiece;
            set {
                if (m_selectedPiece != value) {
                    if (value >= 0 && value < 13) {
                        if (m_selectedPiece != -1) {
                            m_borders[m_selectedPiece].BorderBrush = Background;
                        }
                        m_selectedPiece = value;
                        if (m_selectedPiece != -1) {
                            m_borders[m_selectedPiece].BorderBrush = MainBorder.BorderBrush;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the currently selected piece
        /// </summary>
        public ChessBoard.PieceType SelectedPiece {
            get {
                ChessBoard.PieceType retVal = ChessBoard.PieceType.None;
                int                  selectedIndex;
                
                selectedIndex = SelectedIndex;
                if (selectedIndex > 0 && selectedIndex < 13) {
                    selectedIndex--;
                    if ((selectedIndex & 1) != 0) {
                        retVal |= ChessBoard.PieceType.Black;
                    }
                    selectedIndex >>= 1;
                    switch(selectedIndex) {
                    case 0:
                        retVal |= ChessBoard.PieceType.King;
                        break;
                    case 1:
                        retVal |= ChessBoard.PieceType.Queen;
                        break;
                    case 2:
                        retVal |= ChessBoard.PieceType.Rook;
                        break;
                    case 3:
                        retVal |= ChessBoard.PieceType.Bishop;
                        break;
                    case 4:
                        retVal |= ChessBoard.PieceType.Knight;
                        break;
                    case 5:
                        retVal |= ChessBoard.PieceType.Pawn;
                        break;
                    default:
                        retVal = ChessBoard.PieceType.None;
                        break;
                    }
                }
                return retVal;
            }
        }

        /// <summary>
        /// Select the design mode
        /// </summary>
        public bool BoardDesignMode {
            get => m_isDesignMode;
            set {
                if (m_isDesignMode != value) {
                    SelectedIndex = -1;
                    m_isDesignMode = value;
                    Refresh(isFullRefresh: false);
                    if (m_isDesignMode) {
                        SelectedIndex   = 0;
                    }
                }
            }
        }

        /// <summary>
        /// Called when one of the mouse button is release
        /// </summary>
        /// <param name="e">        Event</param>
        protected override void OnMouseUp(MouseButtonEventArgs e) {
            Point pt;
            int   rowPos;
            int   colPos;
            int   pos;

            base.OnMouseUp(e);
            if (m_isDesignMode) {
                pt     = e.GetPosition(this);
                rowPos = (int)(pt.Y * 4 / ActualHeight);
                colPos = (int)(pt.X * 4 / ActualWidth);
                if (rowPos >= 0 && rowPos < 4 && colPos >= 0 && colPos < 4) {
                    pos            = (rowPos << 2) + colPos;
                    SelectedIndex  = (pos < 13) ? pos : 0;
                }
            }
        }
    } // Class LostPiecesControl
} // Namespace
