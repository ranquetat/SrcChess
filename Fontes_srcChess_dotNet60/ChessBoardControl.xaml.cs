using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.IO;
using Microsoft.Win32;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.ComponentModel;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.Globalization;
using GenericSearchEngine;
using SrcChess2.Core;
using SrcChess2.PgnParsing;

namespace SrcChess2
{
    /// <summary>
    /// Defines a Chess Board Control
    /// </summary>
    public partial class ChessBoardControl : UserControl, ISearchTrace<Move>, IXmlSerializable {

        #region Inner Class
        /// <summary>
        /// Integer Point structure
        /// </summary>
        public struct IntPoint {
            /// <summary>
            /// Class Ctor
            /// </summary>
            /// <param name="x"></param>
            /// <param name="y"></param>
            public  IntPoint(int x, int y) { X = x; Y = y; }
            /// <summary>X point</summary>
            public  int X;
            /// <summary>Y point</summary>
            public  int Y;
        }

        /// <summary>
        /// Arguments for the Reset event
        /// </summary>
        public class NewMoveEventArgs : EventArgs {
            /// <summary>Move which has been done</summary>
            public MoveExt               Move { get; private set; }
            /// <summary>Move result</summary>
            public ChessBoard.GameResult MoveResult { get; private set; }

            /// <summary>
            /// Ctor
            /// </summary>
            /// <param name="move">                 Move</param>
            /// <param name="moveResult">          Move result</param>
            public NewMoveEventArgs(MoveExt move, ChessBoard.GameResult moveResult) {
                Move       = move;
                MoveResult = moveResult;
            }
        }
        
        /// <summary>
        /// Interface implemented by the UI which show the lost pieces.
        /// This interface is called each time the chess board need an update on the lost pieces UI.
        /// </summary>
        public interface IUpdateCmd {
            /// <summary>Update the lost pieces</summary>
            void        Update();
        }

        /// <summary>
        /// Show a piece moving from starting to ending point
        /// </summary>
        private class SyncFlash {
            /// <summary>Chess Board Control</summary>
            private readonly ChessBoardControl m_chessBoardControl;
            /// <summary>Solid Color Brush to flash</summary>
            private readonly SolidColorBrush   m_brush;
            /// <summary>First Flash Color</summary>
            private Color                      m_startColor;
            /// <summary>Second Flash Color</summary>
            private Color                      m_endColor;
            /// <summary>Dispatcher Frame. Wait for flash</summary>
            private DispatcherFrame?           m_dispatcherFrame;

            /// <summary>
            /// Class Ctor
            /// </summary>
            /// <param name="chessBoardControl"> Chess Board Control</param>
            /// <param name="brush">             Solid Color Brush to flash</param>
            /// <param name="colorStart">        First flashing color</param>
            /// <param name="colorEnd">          Second flashing color</param>
            public SyncFlash(ChessBoardControl chessBoardControl, SolidColorBrush brush, Color colorStart, Color colorEnd) {
                m_chessBoardControl = chessBoardControl;
                m_brush             = brush;
                m_startColor        = colorStart;
                m_endColor          = colorEnd;
            }

            /// <summary>
            /// Flash the specified cell
            /// </summary>
            /// <param name="count">                  Flash count</param>
            /// <param name="sec">                    Flash duration</param>
            /// <param name="eventHandlerTerminated"> Event handler to call when flash is finished</param>
            private void FlashCell(int count, double sec, EventHandler eventHandlerTerminated) {
                ColorAnimation animationColor;

                animationColor = new ColorAnimation(m_startColor, m_endColor, new Duration(TimeSpan.FromSeconds(sec))) {
                    AutoReverse    = true,
                    RepeatBehavior = new RepeatBehavior(count / 2)
                };
                if (eventHandlerTerminated != null) {
                    animationColor.Completed += new EventHandler(eventHandlerTerminated);
                }
                m_brush.BeginAnimation(SolidColorBrush.ColorProperty, animationColor);
            }

            /// <summary>
            /// Show the move
            /// </summary>
            public void Flash() {
                m_chessBoardControl.IsEnabled = false;
                FlashCell(4, 0.15, new EventHandler(FirstFlash_Completed));
                m_dispatcherFrame = new DispatcherFrame();
                Dispatcher.PushFrame(m_dispatcherFrame);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="sender">   Sender object</param>
            /// <param name="e">        Event arguments</param>
            private void FirstFlash_Completed(object? sender, EventArgs e) {
                m_chessBoardControl.IsEnabled = true;
                m_dispatcherFrame!.Continue   = false;

            }
        } // Class SyncFlash

        /// <summary>Event argument for the MoveSelected event</summary>
        public class MoveSelectedEventArgs : EventArgs {
            /// <summary>Move position</summary>
            public MoveExt Move;
            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="move">     Move position</param>
            public MoveSelectedEventArgs(MoveExt move) => Move = move;
        }

        /// <summary>Event argument for the QueryPiece event</summary>
        public class QueryPieceEventArgs : EventArgs {
            /// <summary>Position of the square</summary>
            public int                  Pos { get; private set; }
            /// <summary>Piece</summary>
            public ChessBoard.PieceType PieceType { get; set; }

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="pos">          Position of the square</param>
            /// <param name="defPieceType"> Piece type</param>
            public QueryPieceEventArgs(int pos, ChessBoard.PieceType defPieceType) { Pos = pos; PieceType = defPieceType; }
        }

        /// <summary>Event argument for the QueryPawnPromotionType event</summary>
        public class QueryPawnPromotionTypeEventArgs : EventArgs {
            /// <summary>Promotion type (Queen, Rook, Bishop, Knight or Pawn)</summary>
            public Move.MoveType                 PawnPromotionType { get; set; }
            /// <summary>Possible pawn promotions in the current context</summary>
            public ChessBoard.ValidPawnPromotion ValidPawnPromotion { get; private set; }

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="validPawnPromotion"> Possible pawn promotions in the current context</param>
            public QueryPawnPromotionTypeEventArgs(ChessBoard.ValidPawnPromotion validPawnPromotion) {
                ValidPawnPromotion = validPawnPromotion;
                PawnPromotionType  = Move.MoveType.Normal;
            }
        }

        /// <summary>Cookie for FindBestMove method</summary>
        /// <typeparam name="T">Original cookie type</typeparam>
        private class FindBestMoveCookie<T> {
            /// <summary>Action to trigger when the move is found</summary>
            public Action<T,object?> MoveFoundAction { get; private set; }
            /// <summary>Cookie to be used by the action</summary>
            public T                 Cookie { get; private set; }
            /// <summary>Timestamp when the search has started</summary>            
            public DateTime          TimeSearchStarted { get; private set; }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="moveFoundAction"> Action to be executed when the best move has been found</param>
            /// <param name="cookie">          Cookie to pass to the action</param>
            public FindBestMoveCookie(Action<T, object?> moveFoundAction, T cookie) {
                MoveFoundAction   = moveFoundAction;
                Cookie            = cookie;
                TimeSearchStarted = DateTime.Now;
            }
        }
        #endregion

        #region Members
        /// <summary>Lite Cell Color property</summary>
        public static readonly DependencyProperty         LiteCellColorProperty;
        /// <summary>Dark Cell Color property</summary>
        public static readonly DependencyProperty         DarkCellColorProperty;
        /// <summary>White Pieces Color property</summary>
        public static readonly DependencyProperty         WhitePieceColorProperty;
        /// <summary>Black Pieces Color property</summary>
        public static readonly DependencyProperty         BlackPieceColorProperty;
        /// <summary>Determine if a move is flashing</summary>
        public static readonly  DependencyProperty        MoveFlashingProperty;

        /// <summary>Called when a user select a valid move to be done</summary>
        public event EventHandler<MoveSelectedEventArgs>? MoveSelected;
        /// <summary>Triggered when the board is being reset</summary>
        public event EventHandler<EventArgs>?             BoardReset;
        /// <summary>Called when a new move has been done</summary>
        public event EventHandler<NewMoveEventArgs>?      NewMove;
        /// <summary>Called when the redo position has been changed</summary>
        public event EventHandler?                        RedoPosChanged;
        /// <summary>Delegate for the QueryPiece event</summary>
        public delegate void                              QueryPieceEventHandler(object sender, QueryPieceEventArgs e);
        /// <summary>Called when chess control in design mode need to know which piece to insert in the board</summary>
        public event QueryPieceEventHandler?              QueryPiece;
        /// <summary>Delegate for the QueryPawnPromotionType event</summary>
        public delegate void                              QueryPawnPromotionTypeEventHandler(object sender, QueryPawnPromotionTypeEventArgs e);
        /// <summary>Called when chess control needs to know which type of pawn promotion must be done</summary>
        public event QueryPawnPromotionTypeEventHandler?  QueryPawnPromotionType;
        /// <summary>Called to refreshed the command state (menu, toolbar etc.)</summary>
        public event EventHandler?                        UpdateCmdState;
        /// <summary>Triggered when find move begin</summary>
        public event EventHandler?                        FindMoveBegin;
        /// <summary>Triggered when find move end</summary>
        public event EventHandler?                        FindMoveEnd;

        /// <summary>Message for Control is busy exception</summary>
        private const string                              m_ctlIsBusyMsg = "Control is busy";
        /// <summary>Piece Set to use</summary>
        private PieceSet?                                 m_pieceSet;
        /// <summary>Board</summary>
        private ChessBoard                                m_board;
        /// <summary>Array of frames containing the chess piece</summary>
        private readonly Border[]                         m_borders;
        /// <summary>Array containing the current piece type</summary>
        private readonly ChessBoard.PieceType[]           m_pieceType;
        /// <summary>true to have white in the bottom of the screen, false to have black</summary>
        private bool                                      m_whiteInBottom = true;
        /// <summary>Currently selected cell</summary>
        private IntPoint                                  m_selectedCell;
        /// <summary>Not zero when board is flashing and reentrance can be a problem</summary>
        private int                                       m_busyCount;
        /// <summary>Signal that a move has been completed</summary>
        private readonly System.Threading.EventWaitHandle m_actionDoneSignal;
        #endregion

        #region Board creation
        /// <summary>
        /// Static Ctor
        /// </summary>
        static ChessBoardControl() {
            LiteCellColorProperty   = DependencyProperty.Register("LiteCellColor",
                                                                  typeof(Color),
                                                                  typeof(ChessBoardControl),
                                                                  new FrameworkPropertyMetadata(Colors.Moccasin,
                                                                                                FrameworkPropertyMetadataOptions.AffectsRender,
                                                                                                ColorInfoChanged));
            DarkCellColorProperty   = DependencyProperty.Register("DarkCellColor",
                                                                  typeof(Color),
                                                                  typeof(ChessBoardControl),
                                                                  new FrameworkPropertyMetadata(Colors.SaddleBrown,
                                                                                                FrameworkPropertyMetadataOptions.AffectsRender,
                                                                                                ColorInfoChanged));
            WhitePieceColorProperty = DependencyProperty.Register("WhitePieceColor",
                                                                  typeof(Color),
                                                                  typeof(ChessBoardControl),
                                                                  new FrameworkPropertyMetadata(Colors.White,
                                                                                                FrameworkPropertyMetadataOptions.AffectsRender,
                                                                                                ColorInfoChanged));
            BlackPieceColorProperty = DependencyProperty.Register("BlackPieceColor",
                                                                  typeof(Color),
                                                                  typeof(ChessBoardControl),
                                                                  new FrameworkPropertyMetadata(Colors.Black,
                                                                                                FrameworkPropertyMetadataOptions.AffectsRender,
                                                                                                ColorInfoChanged));
            MoveFlashingProperty    = DependencyProperty.Register("MoveFlashing", 
                                                                  typeof(bool),
                                                                  typeof(ChessBoardControl), 
                                                                  new FrameworkPropertyMetadata(true));
        }

        /// <summary>
        /// Class Ctor
        /// </summary>
        public ChessBoardControl() {
            InitializeComponent();
            m_busyCount        = 0;
            m_actionDoneSignal = new System.Threading.EventWaitHandle(false, System.Threading.EventResetMode.ManualReset);
            m_board            = new ChessBoard(this, Dispatcher);
            m_selectedCell     = new IntPoint(-1, -1);
            m_borders          = new Border[64];
            m_pieceType        = new ChessBoard.PieceType[64];
            AutoSelection      = true;
            GameTimer          = new GameTimer {
                Enabled = false
            };
            GameTimer.Reset(m_board.CurrentPlayer);
            WhitePlayerName    = "Player 1";
            BlackPlayerName    = "Player 2";
            WhitePlayerType    = PgnPlayerType.Human;
            BlackPlayerType    = PgnPlayerType.Human;
            ChessSearchSetting = new ChessSearchSetting();
            InitCell();
            IsDirty            = false;
        }

        /// <summary>
        /// Returns the XML schema if any
        /// </summary>
        /// <returns>
        /// null
        /// </returns>
        public System.Xml.Schema.XmlSchema? GetSchema() => null;

        /// <summary>
        /// Deserialized the control from a XML reader
        /// </summary>
        /// <param name="reader">   Reader</param>
        public void ReadXml(XmlReader reader) {
            string whitePlayerName;
            string blackPlayerName;
            long   whiteTicksCount;
            long   blackTicksCount;

            if (reader.MoveToContent() != XmlNodeType.Element || reader.LocalName != "SrcChess2") {
                throw new SerializationException("Unknown format");
            } else if (reader.GetAttribute("Version") != "1.00") {
                throw new SerializationException("Unknown version");
            } else {
                whitePlayerName = reader.GetAttribute("WhitePlayerName") ?? "White Player";
                blackPlayerName = reader.GetAttribute("BlackPlayerName") ?? "Black Player";
                whiteTicksCount = Int64.Parse(reader.GetAttribute("WhiteTicksCount") ?? "0", CultureInfo.InvariantCulture);
                blackTicksCount = Int64.Parse(reader.GetAttribute("BlackTicksCount") ?? "0", CultureInfo.InvariantCulture);
                reader.ReadStartElement();
                ((IXmlSerializable)m_board).ReadXml(reader);
                InitAfterLoad(whitePlayerName, blackPlayerName, whiteTicksCount, blackTicksCount);
                reader.ReadEndElement();
            }
        }

        /// <summary>
        /// Serialize the control into a XML writer
        /// </summary>
        /// <param name="writer">   XML writer</param>
        public void WriteXml(XmlWriter writer) {
            writer.WriteStartElement("SrcChess2");
            writer.WriteAttributeString("Version",         "1.00");
            writer.WriteAttributeString("WhitePlayerName", WhitePlayerName);
            writer.WriteAttributeString("BlackPlayerName", BlackPlayerName);
            writer.WriteAttributeString("WhiteTicksCount", GameTimer.WhitePlayTime.Ticks.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("BlackTicksCount", GameTimer.BlackPlayTime.Ticks.ToString(CultureInfo.InvariantCulture));
            ((IXmlSerializable)m_board).WriteXml(writer);
            writer.WriteEndElement();
            IsDirty = false;
        }

        /// <summary>
        /// Refresh the board color
        /// </summary>
        private void RefreshBoardColor() {
            int    pos;
            Border border;
            Brush  darkBrush;
            Brush  liteBrush;

            pos       = 63;
            darkBrush = new SolidColorBrush(DarkCellColor);
            liteBrush = new SolidColorBrush(LiteCellColor);
            for (int y = 0; y < 8; y++) {
                for (int x = 0; x < 8; x++) {
                    border            = m_borders[pos--];
                    border.Background = (((x + y) & 1) == 0) ? liteBrush : darkBrush;
                }
            }
        }

        /// <summary>
        /// Initialize the cell
        /// </summary>
        private void InitCell() {
            int    pos;
            Border border;
            Brush  brushDark;
            Brush  brushLite;

            pos       = 63;
            brushDark = new SolidColorBrush(DarkCellColor);
            brushLite = new SolidColorBrush(LiteCellColor);
            for (int y = 0; y < 8; y++) {
                for (int x = 0; x < 8; x++) {
                    border = new Border {
                        Name            = "Cell" + (pos.ToString()),
                        BorderThickness = new Thickness(0),
                        Background      = (((x + y) & 1) == 0) ? brushLite : brushDark,
                        BorderBrush     = Background
                    };
                    border.SetValue(Grid.ColumnProperty, x);
                    border.SetValue(Grid.RowProperty, y);
                    m_borders[pos]   = border;
                    m_pieceType[pos] = ChessBoard.PieceType.None;
                    CellContainer.Children.Add(border);
                    pos--;
                }
            }
        }

        /// <summary>
        /// Set the chess piece control
        /// </summary>
        /// <param name="boardPos">  Board position</param>
        /// <param name="pieceSet">  Piece set</param>
        /// <param name="pieceType"> Piece type</param>
        private void SetPieceControl(int boardPos, PieceSet pieceSet, ChessBoard.PieceType pieceType) {
            Border       border;
            UserControl? userControlPiece;

            border           = m_borders[boardPos];
            userControlPiece = pieceSet[pieceType];
            if (userControlPiece != null) {
                userControlPiece.Margin = (border.BorderThickness.Top == 0) ? new Thickness(3) : new Thickness(1);
            }
            m_pieceType[boardPos] = pieceType;
            border.Child          = userControlPiece;
        }

        /// <summary>
        /// Refresh the specified cell
        /// </summary>
        /// <param name="boardPos">         Board position</param>
        /// <param name="isFullRefresh">    true to refresh even if its the same piece</param>
        private void RefreshCell(int boardPos, bool isFullRefresh) {
            ChessBoard.PieceType pieceType;

            if (m_board != null && m_pieceSet != null) {
                pieceType = m_board[boardPos];
                if (pieceType != m_pieceType[boardPos] || isFullRefresh) {
                    SetPieceControl(boardPos, m_pieceSet, pieceType);
                }
            }
        }

        /// <summary>
        /// Refresh the specified cell
        /// </summary>
        /// <param name="boardPos">    Board position</param>
        private void RefreshCell(int boardPos) => RefreshCell(boardPos, isFullRefresh: false);

        /// <summary>
        /// Refresh the board
        /// </summary>
        /// <param name="isFullRefresh"> true to refresh even if its the same piece</param>
        private void Refresh(bool isFullRefresh) {
            if (m_board != null && m_pieceSet != null) {
                for (int i = 0; i < 64; i++) {
                    RefreshCell(i, isFullRefresh);
                }
            }
        }

        /// <summary>
        /// Refresh the board
        /// </summary>
        public void Refresh() => Refresh(isFullRefresh: false);

        /// <summary>
        /// Reset the board to the initial condition
        /// </summary>
        public void ResetBoard() {
            m_board.ResetBoard();
            SelectedCell = new IntPoint(-1, -1);
            OnBoardReset(EventArgs.Empty);
            OnUpdateCmdState(EventArgs.Empty);
            GameTimer.Reset(m_board.CurrentPlayer);
            GameTimer.Enabled = false;
            Refresh(isFullRefresh: false);
            IsDirty = false;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Called when Image property changed
        /// </summary>
        private static void ColorInfoChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e) {
            if (obj is ChessBoardControl me && e.OldValue != e.NewValue) {
                me.RefreshBoardColor();
            }
        }

        /// <summary>
        /// Gets or sets the chess search setting
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public ChessSearchSetting ChessSearchSetting { get; set; }

        /// <summary>
        /// Return true if board control has been changed
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public bool IsDirty { get; set; }

        /// <summary>
        /// Image displayed to the button
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Browsable(true)]
        [Bindable(true)]
        [Category("Brushes")]
        [Description("Lite Cell Color")]
        public Color LiteCellColor {
            get => (Color)GetValue(LiteCellColorProperty);
            set => SetValue(LiteCellColorProperty, value);
        }

        /// <summary>
        /// Image displayed to the button
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Browsable(true)]
        [Bindable(true)]
        [Category("Brushes")]
        [Description("Dark Cell Color")]
        public Color DarkCellColor {
            get => (Color)GetValue(DarkCellColorProperty);
            set => SetValue(DarkCellColorProperty, value);
        }

        /// <summary>
        /// Image displayed to the button
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Browsable(true)]
        [Bindable(true)]
        [Category("Brushes")]
        [Description("White Pieces Color")]
        public Color WhitePieceColor {
            get => (Color)GetValue(WhitePieceColorProperty);
            set => SetValue(WhitePieceColorProperty, value);
        }

        /// <summary>
        /// Image displayed to the button
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Browsable(true)]
        [Bindable(true)]
        [Category("Brushes")]
        [Description("Black Pieces Color")]
        public Color BlackPieceColor {
            get => (Color)GetValue(BlackPieceColorProperty);
            set => SetValue(BlackPieceColorProperty, value);
        }

        /// <summary>
        /// Determine if a move is flashing
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Browsable(true)]
        [Category("General")]
        [Description("Determine if a move is flashing")]
        public bool MoveFlashing  {
            get => (bool)GetValue(MoveFlashingProperty);
            set => SetValue(MoveFlashingProperty, value);
        }

        /// <summary>
        /// Current piece set
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
        /// Current chess board
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public ChessBoard Board {
            get => m_board;
            set {
                if (m_board != value) {
                    m_board = value;
                    Refresh(isFullRefresh: false);
                }
            }
        }

        /// <summary>
        /// Signal used to determine if the called action has been done
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public System.Threading.EventWaitHandle SignalActionDone => m_actionDoneSignal;

        /// <summary>
        /// Name of the player playing white piece
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public string WhitePlayerName { get; set; }

        /// <summary>
        /// Name of the player playing black piece
        /// </summary>
        public string BlackPlayerName { get; set; }

        /// <summary>
        /// Type of player playing white piece
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public PgnPlayerType WhitePlayerType { get; set; }

        /// <summary>
        /// Type of player playing black piece
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public PgnPlayerType BlackPlayerType { get; set; }

        /// <summary>
        /// Gets the chess board associated with the control
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public ChessBoard ChessBoard => m_board;

        /// <summary>
        /// Determine if the White are in the top or bottom of the draw board
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public bool WhiteInBottom {
            get => m_whiteInBottom;
            set {
                if (value != m_whiteInBottom) {
                    m_whiteInBottom = value;
                    Refresh(isFullRefresh: false);
                }
            }
        }

        /// <summary>
        /// Enable or disable the auto selection mode
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public bool AutoSelection { get; set; }

        /// <summary>
        /// Determine the board design mode
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public bool BoardDesignMode {
            get => m_board.IsDesignMode;
            set {
                MessageBoxResult       result;
                ChessBoard.PlayerColor nextMoveColor;
                
                if (m_board.IsDesignMode != value) {
                    if (value) {
                        m_board.OpenDesignMode();
                        m_board.MovePosStack.Clear();
                        OnBoardReset(EventArgs.Empty);
                        GameTimer.Enabled = false;
                        OnUpdateCmdState(EventArgs.Empty);
                    } else {
                        result = MessageBox.Show("Is the next move to the white?", "SrcChess", MessageBoxButton.YesNo);
                        nextMoveColor = (result == MessageBoxResult.Yes) ? ChessBoard.PlayerColor.White : ChessBoard.PlayerColor.Black;
                        if (m_board.CloseDesignMode(nextMoveColor, (ChessBoard.BoardStateMask)0, 0 /*iEnPassant*/)) {
                            OnBoardReset(EventArgs.Empty);
                            GameTimer.Reset(m_board.CurrentPlayer);
                            GameTimer.Enabled = true;
                            IsDirty           = true;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the number of move which can be undone
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public int UndoCount => m_board.MovePosStack.PositionInList + 1;

        /// <summary>
        /// Gets the number of move which can be redone
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public int RedoCount => m_board.MovePosStack.Count - m_board.MovePosStack.PositionInList - 1;

        /// <summary>
        /// Current color to play
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public ChessBoard.PlayerColor NextMoveColor => m_board.CurrentPlayer;

        /// <summary>
        /// List of played moves
        /// </summary>
        private MoveExt[] MoveList {
            get {
                MoveExt[] moves;
                int       moveCount;
                
                moveCount   = m_board.MovePosStack.PositionInList + 1;
                moves       = new MoveExt[moveCount];
                if (moveCount != 0) {
                    m_board.MovePosStack.List.CopyTo(0, moves, 0, moveCount);
                }
                return moves;
            }
        }

        /// <summary>
        /// Game timer
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public GameTimer GameTimer { get; private set; }

        /// <summary>
        /// Currently selected case
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public IntPoint SelectedCell {
            get => m_selectedCell;
            set {
                SetCellSelectionState(m_selectedCell, false);
                m_selectedCell = value;
                SetCellSelectionState(m_selectedCell, true);
            }
        }

        /// <summary>
        /// true if a cell is selected
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public bool IsCellSelected => SelectedCell.X != -1 || SelectedCell.Y != -1;

        /// <summary>
        /// Return true if board is flashing and we must not let the control be reentered
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public bool IsBusy => m_busyCount != 0;

        /// <summary>
        /// Return true if we're observing a game from a chess server
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public bool IsObservingAGame { get; set; }

        /// <summary>
        /// Returns if the search engine is busy
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public static bool IsSearchEngineBusy => SearchEngine<ChessGameBoardAdaptor,Move>.IsSearchEngineBusy;

        /// <summary>
        /// Returns if the running search for best move has been canceled
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public static bool IsSearchCancel => SearchEngine<ChessGameBoardAdaptor,Move>.IsSearchHasBeenCanceled;
        #endregion

        #region Events
        /// <summary>
        /// Trigger the FindMoveBegin event.
        /// </summary>
        /// <param name="e"> Event argument</param>
        protected void OnFindMoveBegin(EventArgs e) => FindMoveBegin?.Invoke(this, e);

        /// <summary>
        /// Trigger the FindMoveEnd event.
        /// </summary>
        /// <param name="e"> Event argument</param>
        protected void OnFindMoveEnd(EventArgs e) => FindMoveEnd?.Invoke(this, e);

        /// <summary>
        /// Trigger the UpdateCmdState event. Called when command state need to be reevaluated.
        /// </summary>
        /// <param name="e"> Event argument</param>
        protected void OnUpdateCmdState(EventArgs e) => UpdateCmdState?.Invoke(this, e);

        /// <summary>
        /// Trigger the BoardReset event
        /// </summary>
        /// <param name="e"> Event arguments</param>
        protected void OnBoardReset(EventArgs e) => BoardReset?.Invoke(this, e);

        /// <summary>
        /// Trigger the RedoPosChanged event
        /// </summary>
        /// <param name="e"> Event arguments</param>
        protected void OnRedoPosChanged(EventArgs e) => RedoPosChanged?.Invoke(this, e);

        /// <summary>
        /// Trigger the NewMove event
        /// </summary>
        /// <param name="e"> Event arguments</param>
        protected void OnNewMove(NewMoveEventArgs e) => NewMove?.Invoke(this, e);

        /// <summary>
        /// Trigger the MoveSelected event
        /// </summary>
        /// <param name="e"> Event arguments</param>
        protected virtual void OnMoveSelected(MoveSelectedEventArgs e) => MoveSelected?.Invoke(this, e);

        /// <summary>
        /// Trigger the QueryPiece event
        /// </summary>
        /// <param name="e"> Event arguments</param>
        protected virtual void OnQueryPiece(QueryPieceEventArgs e) => QueryPiece?.Invoke(this, e);

        /// <summary>
        /// Trigger the QueryPawnPromotionType event
        /// </summary>
        /// <param name="e"> Event arguments</param>
        protected virtual void OnQueryPawnPromotionType(QueryPawnPromotionTypeEventArgs e) => QueryPawnPromotionType?.Invoke(this, e);
        #endregion

        #region Methods
        /// <summary>
        /// Show an error message
        /// </summary>
        /// <param name="errMsg"> Error message</param>
        public void ShowError(string errMsg) => MessageBox.Show(Window.GetWindow(this), errMsg, "...", MessageBoxButton.OK, MessageBoxImage.Error);

        /// <summary>
        /// Show a message
        /// </summary>
        /// <param name="msg"> Message</param>
        public void ShowMessage(string msg) => MessageBox.Show(Window.GetWindow(this), msg, "...", MessageBoxButton.OK, MessageBoxImage.Information);

        /// <summary>
        /// Set the cell selection  appearance
        /// </summary>
        /// <param name="cell">       Cell position</param>
        /// <param name="isSelected"> true if selected, false if not</param>
        private void SetCellSelectionState(IntPoint cell, bool isSelected) {
            Border border;
            int    pos;

            if (cell.X != -1 && cell.Y != -1) {
                pos                    = cell.X + cell.Y * 8;
                border                 = m_borders[pos];
                border.BorderBrush     = (isSelected) ? Brushes.Black : border.Background;
                border.BorderThickness = (isSelected) ? new Thickness(1) : new Thickness(0);
                if (border.Child is Control ctl ) {
                    ctl.Margin  = (isSelected) ? new Thickness(1) : new Thickness(3);
                }
            }
        }

        /// <summary>
        /// Save the current game into a file
        /// </summary>
        /// <param name="writer">   Binary writer</param>
        public virtual void SaveGame(BinaryWriter writer) {
            string  version;
            
            version = "SRCBC095";
            writer.Write(version);
            m_board.SaveBoard(writer);
            writer.Write(WhitePlayerName);
            writer.Write(BlackPlayerName);
            writer.Write(GameTimer.WhitePlayTime.Ticks);
            writer.Write(GameTimer.BlackPlayTime.Ticks);
            IsDirty = false;
        }

        /// <summary>
        /// Initialize the board control after a board has been loaded
        /// </summary>
        private void InitAfterLoad(string whitePlayerName, string blackPlayerName, long whiteTicks, long blackTicks) {
            OnBoardReset(EventArgs.Empty);
            OnUpdateCmdState(EventArgs.Empty);
            Refresh(isFullRefresh: false);
            WhitePlayerName = whitePlayerName;
            BlackPlayerName = blackPlayerName;
            IsDirty         = false;
            GameTimer.ResetTo(m_board.CurrentPlayer, whiteTicks, blackTicks);
            GameTimer.Enabled = true;
        }

        /// <summary>
        /// Load a game from a stream
        /// </summary>
        /// <param name="reader">   Binary reader</param>
        public virtual bool LoadGame(BinaryReader reader) {
            bool    retVal;
            string  version;
            string  whitePlayerName;
            string  blackPlayerName;
            long    whiteTicks;
            long    blackTicks;
            
            version = reader.ReadString();
            if (version != "SRCBC095") {
                retVal = false;
            } else {
                retVal = m_board.LoadBoard(reader);
                if (retVal) {
                    whitePlayerName = reader.ReadString();
                    blackPlayerName = reader.ReadString();
                    whiteTicks      = reader.ReadInt64();
                    blackTicks      = reader.ReadInt64();
                    InitAfterLoad(whitePlayerName, blackPlayerName, whiteTicks, blackTicks);
                }
            }
            return retVal;
        }

        /// <summary>
        /// Save the board content into a snapshot string.
        /// </summary>
        /// <returns>
        /// Snapshot
        /// </returns>
        public string TakeSnapshot() {
            StringBuilder     retVal;
            XmlWriter         writer;
            XmlWriterSettings xmlSettings;

            retVal = new StringBuilder(16384);
            xmlSettings = new XmlWriterSettings {
                Indent = true
            };
            writer = XmlWriter.Create(retVal, xmlSettings);
            ((IXmlSerializable)this).WriteXml(writer);
            writer.Close();
            return retVal.ToString();
        }

        /// <summary>
        /// Restore the snapshot
        /// </summary>
        /// <param name="snapshot"> Snapshot</param>
        public void RestoreSnapshot(string snapshot) {
            TextReader  textReader;
            XmlReader   reader;

            textReader = new StringReader(snapshot);
            reader     = XmlReader.Create(textReader);
            ((IXmlSerializable)this).ReadXml(reader);
        }

        /// <summary>
        /// Load a board from a file selected by the user.
        /// </summary>
        /// <returns>
        /// true if a new board has been read
        /// </returns>
         public bool LoadFromFile() {
            bool             retVal = false;
            OpenFileDialog   openDlg;
            Stream?          stream;
            BinaryReader     reader;
            FrmPgnGamePicker pickerFrm;
            int              index;
            string           snapshot;

            openDlg = new OpenFileDialog {
                AddExtension    = true,
                CheckFileExists = true,
                CheckPathExists = true,
                DefaultExt      = "che",
                Filter          = "Chess Files (*.che, *.pgn, *.cbsnp)|*.che;*.pgn;*.cbsnp",
                Multiselect     = false
            };
            if (openDlg.ShowDialog() == true) {
                index = openDlg.FileName.LastIndexOf('.');
                if (index != -1 && openDlg.FileName[index..].ToLower() == ".cbsnp") {
                    try {
                        snapshot = File.ReadAllText(openDlg.FileName);
                        RestoreSnapshot(snapshot);
                    } catch (Exception ex) {
                        ShowError(ex.Message);
                    }
                } else if (index == -1 || openDlg.FileName[index..].ToLower() != ".pgn") {
                    try {
                        stream = openDlg.OpenFile();
                    } catch(Exception) {
                        ShowError($"Unable to open the file - {openDlg.FileName}");
                        stream = null;
                    }
                    if (stream != null) {
                        try {
                            using (reader = new BinaryReader(stream)) {
                                if (!LoadGame(reader)) {
                                    ShowError($"Bad file version - {openDlg.FileName}");
                                } else {
                                    retVal = true;
                                }
                            }
                        } catch(SystemException) {
                            ShowError($"The file '{openDlg.FileName}' seems to be corrupted.");
                            ResetBoard();
                        }
                        OnUpdateCmdState(EventArgs.Empty);
                    }
                } else {
                    pickerFrm = new FrmPgnGamePicker();
                    if (pickerFrm.InitForm(openDlg.FileName)) {
                        if (pickerFrm.ShowDialog() == true) {
                            CreateGameFromMove(pickerFrm.StartingChessBoard,
                                               pickerFrm.MoveList!,
                                               pickerFrm.StartingColor,
                                               pickerFrm.WhitePlayerName,
                                               pickerFrm.BlackPlayerName,
                                               pickerFrm.WhitePlayerType,
                                               pickerFrm.BlackPlayerType,
                                               pickerFrm.WhiteTimer,
                                               pickerFrm.BlackTimer);
                            retVal = true;
                        }
                    }
                }
            }
            if (retVal) {
                IsDirty = false;
            }
            return retVal;
        }

        /// <summary>
        /// Save a board to a file selected by the user
        /// </summary>
        /// <returns>
        /// true if the game has been saved
        /// </returns>
        public bool SaveToFile() {
            bool           retVal = false;
            SaveFileDialog saveDlg;
            Stream?        stream;

            saveDlg = new SaveFileDialog {
                AddExtension    = true,
                CheckPathExists = true,
                DefaultExt      = "che",
                Filter          = "Chess Files (*.che)|*.che",
                OverwritePrompt = true
            };
            if (saveDlg.ShowDialog() == true) {
                try {
                    stream = saveDlg.OpenFile();
                } catch(Exception) {
                    ShowError($"Unable to open the file - {saveDlg.FileName}");
                    stream = null;
                }
                if (stream != null) {
                    try {
                        SaveGame(new BinaryWriter(stream));
                        retVal  = true;
                        IsDirty = false;
                    } catch(SystemException) {
                        ShowError($"Unable to write to the file '{saveDlg.FileName}'.");
                    }
                    stream.Dispose();
                }
            }
            return retVal;
        }

        /// <summary>
        /// Save the board to a file selected by the user in PGN format
        /// </summary>
        public void SavePgnToFile() {
            SaveFileDialog   saveDlg;
            Stream?          stream;
            StreamWriter     writer;
            MessageBoxResult result;

            saveDlg = new SaveFileDialog {
                AddExtension    = true,
                CheckPathExists = true,
                DefaultExt      = "pgn",
                Filter          = "PGN Chess Files (*.pgn)|*.pgn",
                OverwritePrompt = true
            };
            if (saveDlg.ShowDialog() == true) {
                if (m_board.MovePosStack.PositionInList + 1 != m_board.MovePosStack.List.Count) {
                    result = MessageBox.Show("Do you want to save the undone moves?", "Saving to PGN File", MessageBoxButton.YesNoCancel);
                } else {
                    result = MessageBoxResult.Yes;
                }
                if (result != MessageBoxResult.Cancel) {
                    try {
                        stream = saveDlg.OpenFile();
                    } catch(Exception) {
                        ShowError($"Unable to open the file - {saveDlg.FileName}");
                        stream = null;
                    }
                    if (stream != null) {
                        writer = new StreamWriter(stream, Encoding.GetEncoding("Utf-8"));
                        try {
                            using (writer) {
                                writer.Write(SaveGameToPgnText(result == MessageBoxResult.Yes));
                                IsDirty = false;
                            }
                        } catch(SystemException) {
                            ShowError($"Unable to write to the file '{saveDlg.FileName}'.");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Save the board to a file selected by the user in PGN format
        /// </summary>
        public void SaveSnapshot() {
            SaveFileDialog saveDlg;
            string         strSnapshot;

            saveDlg = new SaveFileDialog {
                AddExtension    = true,
                CheckPathExists = true,
                DefaultExt      = "pgn",
                Filter          = "Debugging Snapshot File (*.cbsnp)|*.cbsnp",
                OverwritePrompt = true
            };
            if (saveDlg.ShowDialog() == true) {
                strSnapshot = TakeSnapshot();
                try {
                    File.WriteAllText(saveDlg.FileName, strSnapshot, Encoding.Unicode);
                } catch(SystemException) {
                    ShowError($"Unable to write to the file '{saveDlg.FileName}'.");
                }
            }
        }

        /// <summary>
        /// Create a new game using the specified list of moves
        /// </summary>
        /// <param name="startingChessBoard"> Starting board or null if standard board</param>
        /// <param name="moveList">           List of moves</param>
        /// <param name="nextMoveColor">      Color starting to play</param>
        /// <param name="whitePlayerName">    Name of the player playing white pieces</param>
        /// <param name="blackPlayerName">    Name of the player playing black pieces</param>
        /// <param name="whitePlayerType">    Type of player playing white pieces</param>
        /// <param name="blackPlayerType">    Type of player playing black pieces</param>
        /// <param name="whitePlayerSpan">    Timer for white</param>
        /// <param name="blackPlayerSpan">    Timer for black</param>
        public virtual void CreateGameFromMove(ChessBoard?            startingChessBoard,
                                               List<MoveExt>          moveList,
                                               ChessBoard.PlayerColor nextMoveColor,
                                               string                 whitePlayerName,
                                               string                 blackPlayerName,
                                               PgnPlayerType          whitePlayerType,
                                               PgnPlayerType          blackPlayerType,
                                               TimeSpan               whitePlayerSpan,
                                               TimeSpan               blackPlayerSpan) {
            m_board.CreateGameFromMove(startingChessBoard, moveList, nextMoveColor);
            OnBoardReset(EventArgs.Empty);
            WhitePlayerName = whitePlayerName;
            BlackPlayerName = blackPlayerName;
            WhitePlayerType = whitePlayerType;
            BlackPlayerType = blackPlayerType;
            OnUpdateCmdState(EventArgs.Empty);
            GameTimer.ResetTo(m_board.CurrentPlayer,
                              whitePlayerSpan.Ticks,
                              blackPlayerSpan.Ticks);
            GameTimer.Enabled = true;
            IsDirty           = false;
            Refresh(isFullRefresh: false);
        }

        /// <summary>
        /// Creates a game from a PGN text paste by the user
        /// </summary>
        /// <returns>
        /// true if a new board has been loaded
        /// </returns>
        public bool CreateFromPgnText() {
            bool             retVal = false;
            FrmCreatePgnGame frm;

            frm = new FrmCreatePgnGame {
                Owner = Window.GetWindow(this)
            };
            if (frm.ShowDialog() == true) {
                CreateGameFromMove(frm.StartingChessBoard,
                                   frm.MoveList!,
                                   frm.StartingColor,
                                   frm.WhitePlayerName ?? "",
                                   frm.BlackPlayerName ?? "",
                                   frm.WhitePlayerType,
                                   frm.BlackPlayerType,
                                   frm.WhiteTimer,
                                   frm.BlackTimer);
                retVal = true;
            }
            return retVal;
        }

        /// <summary>
        /// Creates a game from a PGN text paste by the user
        /// </summary>
        /// <param name="includeRedoMove">  true to include redo move</param>
        /// <returns>
        /// true if a new board has been loaded
        /// </returns>
         public string SaveGameToPgnText(bool includeRedoMove)
            => PgnUtil.GetPgnFromBoard(m_board,
                                       includeRedoMove,
                                       "SrcChess Game",
                                       "SrcChess Location",
                                       DateTime.Now.ToString("yyyy.MM.dd"),
                                       "1",
                                       WhitePlayerName,
                                       BlackPlayerName,
                                       WhitePlayerType,
                                       BlackPlayerType,
                                       GameTimer.WhitePlayTime,
                                       GameTimer.BlackPlayTime);

        /// <summary>
        /// Create a book from files selected by the user
        /// </summary>
         public void CreateBookFromFiles() {
            OpenFileDialog         openDlg;
            SaveFileDialog         saveDlg;
            Book                   book;
            FrmCreatingBookFromPgn pgnParsingWnd;
            List<short[]>          moveList;
            string                 errTxt;
            int                    bookEntries;
            int                    totalSkipped;
            int                    totalTruncated;

            openDlg = new OpenFileDialog {
                AddExtension    = true,
                CheckFileExists = true,
                CheckPathExists = true,
                DefaultExt      = "pgn",
                Filter          = "Chess PGN Files (*.pgn)|*.pgn",
                Multiselect     = true
            };
            if (openDlg.ShowDialog() == true) {
                pgnParsingWnd = new FrmCreatingBookFromPgn(openDlg.FileNames) {
                    Owner = MainWindow.GetWindow(this)
                };
                if (pgnParsingWnd.ShowDialog() == false) {
                    errTxt    = pgnParsingWnd.Error ?? "Cancelled by the user";
                    ShowError(errTxt);
                } else {
                    totalSkipped   = pgnParsingWnd.TotalSkipped;
                    totalTruncated = pgnParsingWnd.TotalTruncated;
                    book           = pgnParsingWnd.Book!;
                    bookEntries    = pgnParsingWnd.BookEntryCount;
                    moveList       = pgnParsingWnd.MoveList!;
                    ShowMessage($"{openDlg.FileNames.Length} PNG file(s) read. {moveList.Count} games processed. {totalTruncated} truncated. {totalSkipped} skipped. {bookEntries} book entries defined.");
                    saveDlg = new SaveFileDialog {
                        AddExtension    = true,
                        CheckPathExists = true,
                        DefaultExt      = "bin",
                        Filter          = "Chess Opening Book (*.bin)|*.bin",
                        OverwritePrompt = true
                    };
                    if (saveDlg.ShowDialog() == true) {
                        try {
                            book.SaveBookToFile(saveDlg.FileName);
                        } catch (Exception ex) {
                            ShowError(ex.Message);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Find a move from the opening book
        /// </summary>
        /// <param name="book"> Book</param>
        /// <param name="move"> Found move</param>
        /// <returns>
        /// true if succeed, false if no move found in book
        /// </returns>
        public bool FindBookMove(Book book, out Move move) {
            bool      retVal;
            MoveExt[] moves;
            
            if (!m_board.IsStdInitialBoard) {
                move.OriginalPiece = ChessBoard.PieceType.None;
                move.StartPos      = 255;
                move.EndPos        = 255;
                move.Type          = Move.MoveType.Normal;
                retVal             = false;
            } else {
                moves  = MoveList;
                retVal = m_board.FindBookMove(book, ChessSearchSetting, m_board.CurrentPlayer, moves, out move);
            }
            return retVal;
        }

        /// <summary>
        /// Call back the original action after update the move time counter
        /// </summary>
        /// <typeparam name="T">          Type of the original cookie</typeparam>
        /// <param name="cookieCallBack"> Call back cookie</param>
        /// <param name="move">           Found move</param>
        private void FindBestMoveEnd<T>(FindBestMoveCookie<T> cookieCallBack, object? move) {
            OnFindMoveEnd(EventArgs.Empty);
            if (move is MoveExt moveExt) {
                moveExt.TimeToCompute = DateTime.Now - cookieCallBack.TimeSearchStarted;
                cookieCallBack.MoveFoundAction(cookieCallBack.Cookie, moveExt);
            }
        }

        /// <summary>
        /// Find the best move for a player using alpha-beta pruning or minmax search
        /// </summary>
        /// <param name="chessBoard">      Chess board to use. Null to use the base one</param>
        /// <param name="moveFoundAction"> Action to do with the move found</param>
        /// <param name="cookie">          Cookie to pass to the action</param>
        /// <returns>
        /// true if succeed, false if thread is busy
        /// </returns>
        public bool FindBestMove<T>(ChessBoard?       chessBoard,
                                    Action<T,object?> moveFoundAction,
                                    T                 cookie) {
            bool                  retVal;
            Book?                 book;
            MoveExt               bestMove;
            FindBestMoveCookie<T> callBackCookie;

            book = ChessSearchSetting.Book;
            if (book != null && FindBookMove(book, out Move move)) {
                bestMove = new MoveExt(move, "", -1, -1, 0, 0);
                moveFoundAction(cookie, bestMove);
                retVal   = true;
            } else {
                chessBoard    ??= m_board;
                callBackCookie  = new FindBestMoveCookie<T>(moveFoundAction, cookie);
                OnFindMoveBegin(EventArgs.Empty);
                retVal          = chessBoard.FindBestMove(m_board.CurrentPlayer,
                                                          ChessSearchSetting,
                                                          (x,y) => FindBestMoveEnd(x,y),
                                                          callBackCookie);
            }
            return retVal;
        }

        /// <summary>
        /// Called when the best move routine is done
        /// </summary>
        /// <param name="callBackCookie"> Call back cookie</param>
        /// <param name="move">           Found move</param>
        private void ShowHintEnd(FindBestMoveCookie<bool> callBackCookie, object? move) {
            if (move is MoveExt moveExt) {
                moveExt.TimeToCompute = DateTime.Now - callBackCookie.TimeSearchStarted;
                callBackCookie.MoveFoundAction(true, moveExt);
                ShowBeforeMove(moveExt, true);
                m_board.DoMoveNoLog(moveExt.Move);
                ShowAfterMove(moveExt, true);
                m_board.UndoMoveNoLog(moveExt.Move);
                ShowAfterMove(moveExt, false);
                callBackCookie.MoveFoundAction(false, moveExt);
            }
        }

        /// <summary>
        /// Show a hint
        /// </summary>
        /// <param name="hintFoundAction">  Action to do with the hint is found</param>
        /// <returns>
        /// true if search has started, false if search engine is busy
        /// </returns>
        public bool ShowHint(Action<bool,object?> hintFoundAction) {
            bool                     retVal;
            FindBestMoveCookie<bool> callBackCookie;

            if (m_busyCount != 0) {
                throw new MethodAccessException(m_ctlIsBusyMsg);
            }
            callBackCookie = new FindBestMoveCookie<bool>(hintFoundAction, true);
            retVal         = FindBestMove(null, (x,y) => ShowHintEnd(x,y), callBackCookie);
            return retVal;
        }

        /// <summary>
        /// Cancel search
        /// </summary>
        public static void CancelSearch() => ChessBoard.CancelSearch();

        /// <summary>
        /// Search trace
        /// </summary>
        /// <param name="depth">    Search depth</param>
        /// <param name="playerId"> Color who play</param>
        /// <param name="move">     Move position</param>
        /// <param name="pts">      Points</param>
        public virtual void LogSearchTrace(int depth, int playerId, Move move, int pts) {}

        /// <summary>
        /// Gets the cell position from a mouse event
        /// </summary>
        /// <param name="e">       Mouse event argument</param>
        /// <param name="cellPos"> Resulting cell position</param>
        /// <returns>
        /// true if succeed, false if mouse don't point to a cell
        /// </returns>
        public bool GetCellFromPoint(MouseEventArgs e, out IntPoint cellPos) {
            bool   retVal;
            Point  pt;
            int    col;
            int    row;
            double actualWidth;
            double actualHeight;

            pt           = e.GetPosition(CellContainer);
            actualHeight = CellContainer.ActualHeight;
            actualWidth  = CellContainer.ActualWidth;
            col          = (int)(pt.X * 8 / actualWidth);
            row          = (int)(pt.Y * 8 / actualHeight);
            if (col >= 0 && col < 8 && row >= 0 && row < 8) {
                cellPos = new IntPoint(7 - col, 7 - row);
                retVal = true;
            } else {
                cellPos = new IntPoint(-1, -1);
                retVal  = false;
            }
            return retVal;
        }

        /// <summary>
        /// Flash the specified cell
        /// </summary>
        /// <param name="cellPos">   Cell to flash</param>
        public void FlashCell(IntPoint cellPos) {
            int       absCellPos;
            Border    border;
            Brush     brush;
            object?     oriBrush;
            Color     colorStart;
            Color     colorEnd;
            SyncFlash syncFlash;
            
            m_busyCount++;  // When flashing, a message loop is processed which can cause reentrance problem
            try { 
                absCellPos = cellPos.X + cellPos.Y * 8;
                if (((cellPos.X + cellPos.Y) & 1) != 0) {
                    colorStart = DarkCellColor;
                    colorEnd   = LiteCellColor;
                } else {
                    colorStart = LiteCellColor;
                    colorEnd   = DarkCellColor;
                }
                border   = m_borders[absCellPos];
                oriBrush = border.Background.ReadLocalValue(BackgroundProperty);
                if (oriBrush == DependencyProperty.UnsetValue) {
                    oriBrush = null;
                }
                brush             = border.Background.Clone();
                border.Background = brush;
                syncFlash         = new SyncFlash(this, (SolidColorBrush)brush, colorStart, colorEnd);
                syncFlash.Flash();
                if (oriBrush == null) {
                    border.Background.ClearValue(BackgroundProperty);
                } else {
                    border.Background = (Brush)oriBrush;
                }
            } finally {
                m_busyCount--;
            }
        }

        /// <summary>
        /// Flash the specified cell
        /// </summary>
        /// <param name="startPos"> Cell position</param>
        private void FlashCell(int startPos) => FlashCell(new IntPoint(startPos & 7, startPos / 8));

        /// <summary>
        /// Get additional position to update when doing or undoing a special move
        /// </summary>
        /// <param name="movePos">  Position of the move</param>
        /// <returns>
        /// Array of position to undo
        /// </returns>
        private int[] GetPosToUpdate(Move movePos) {
            List<int> retVal = new(2);

            if ((movePos.Type & Move.MoveType.MoveTypeMask) == Move.MoveType.Castle) {
                switch(movePos.EndPos) {
                case 1:
                    retVal.Add(0);
                    retVal.Add(2);
                    break;
                case 5:
                    retVal.Add(7);
                    retVal.Add(4);
                    break;
                case 57:
                    retVal.Add(56);
                    retVal.Add(58);
                    break;
                case 61:
                    retVal.Add(63);
                    retVal.Add(60);
                    break;
                default:
                    ShowError("Oops!");
                    break;
                }
            } else if ((movePos.Type & Move.MoveType.MoveTypeMask) == Move.MoveType.EnPassant) {
                retVal.Add((movePos.StartPos & 56) + (movePos.EndPos & 7));
            }
            return retVal.ToArray();
        }

        /// <summary>
        /// Show before move is done
        /// </summary>
        /// <param name="movePos"> Position of the move</param>
        /// <param name="flash">   true to flash the from and destination pieces</param>
        private void ShowBeforeMove(MoveExt movePos, bool flash) {
            if (flash) {
                FlashCell(movePos.Move.StartPos);
            }
        }

        /// <summary>
        /// Show after move is done
        /// </summary>
        /// <param name="movePos"> Position of the move</param>
        /// <param name="flash">   true to flash the from and destination pieces</param>
        private void ShowAfterMove(MoveExt movePos, bool flash) {
            int[] posToUpdate;

            RefreshCell(movePos.Move.StartPos);
            RefreshCell(movePos.Move.EndPos);
            if (flash) {
                FlashCell(movePos.Move.EndPos);
            }
            posToUpdate = GetPosToUpdate(movePos.Move);
            foreach (int pos in posToUpdate) {
                if (flash) {
                    FlashCell(pos);
                }
                RefreshCell(pos);
            }
        }

        /// <summary>
        /// Play the specified move
        /// </summary>
        /// <param name="move">  Position of the move</param>
        /// <param name="flash"> true to flash when doing the move</param>
        /// <returns>
        /// NoRepeat, FiftyRuleRepeat, ThreeFoldRepeat, Tie, Check, Mate
        /// </returns>
        public ChessBoard.GameResult DoMove(MoveExt move, bool flash) {
            ChessBoard.GameResult retVal;

            if (m_busyCount != 0) { 
                throw new MethodAccessException(m_ctlIsBusyMsg);
            }
            if (!m_board.IsMoveValid(move.Move)) {
                throw new ArgumentException("Try to make an illegal move", nameof(move));
            }
            m_actionDoneSignal.Reset();
            ShowBeforeMove(move, flash);
            retVal = m_board.DoMove(move);
            ShowAfterMove(move, flash);
            OnNewMove(new NewMoveEventArgs(move, retVal));
            OnUpdateCmdState(EventArgs.Empty);
            GameTimer.PlayerColor = m_board.CurrentPlayer;
            GameTimer.Enabled     = (retVal == ChessBoard.GameResult.OnGoing || retVal == ChessBoard.GameResult.Check);
            m_actionDoneSignal.Set();
            return retVal;
        }

        /// <summary>
        /// Play the specified move
        /// </summary>
        /// <param name="move"> Position of the move</param>
        /// <returns>
        /// NoRepeat, FiftyRuleRepeat, ThreeFoldRepeat, Tie, Check, Mate
        /// </returns>
        public ChessBoard.GameResult DoMove(MoveExt move) => DoMove(move, MoveFlashing);

        /// <summary>
        /// Play the specified move
        /// </summary>
        /// <param name="move"> Position of the move</param>
        /// <returns>
        /// NoRepeat, FiftyRuleRepeat, ThreeFoldRepeat, Tie, Check, Mate
        /// </returns>
        public ChessBoard.GameResult DoUserMove(MoveExt move) {
            ChessBoard.GameResult  retVal;

            retVal  = DoMove(move);
            IsDirty = true;
            return retVal;
        }

        /// <summary>
        /// Undo the last move
        /// </summary>
        /// <param name="isPlayerAgainstPlayer"> true if player against player</param>
        /// <param name="flash">                 true to flash the from and destination pieces</param>
        private void UndoMove(bool isPlayerAgainstPlayer, bool flash) {
            MoveExt move;
            int[]   posToUpdate;
            int     count;

            if (m_busyCount != 0) { 
                throw new MethodAccessException(m_ctlIsBusyMsg);
            }
            count = isPlayerAgainstPlayer ? 1 : 2;
            if (count <= UndoCount) {
                for (int i = 0; i < count; i++) {
                    move = m_board.MovePosStack.CurrentMove;
                    if (flash) {
                        FlashCell(move.Move.EndPos);
                    }
                    m_board.UndoMove();
                    RefreshCell(move.Move.EndPos);
                    RefreshCell(move.Move.StartPos);
                    if (flash) {
                        FlashCell(move.Move.StartPos);
                    }
                    posToUpdate = GetPosToUpdate(move.Move);
                    Array.Reverse(posToUpdate);
                    foreach (int pos in posToUpdate) {
                        if (flash) {
                            FlashCell(pos);
                        }
                        RefreshCell(pos);
                    }
                    OnRedoPosChanged(EventArgs.Empty);
                    OnUpdateCmdState(EventArgs.Empty);
                    GameTimer.PlayerColor = m_board.CurrentPlayer;
                    GameTimer.Enabled     = true;
                }
            }
        }

        /// <summary>
        /// Undo the last move
        /// </summary>
        /// <param name="isPlayerAgainstPlayer"> true if player against player</param>
        /// <param name="computerColor">         Color played by the computer if any</param>
        public void UndoMove(bool isPlayerAgainstPlayer, ChessBoard.PlayerColor computerColor) {
            bool flash;

            if (!isPlayerAgainstPlayer && computerColor == NextMoveColor) {
                isPlayerAgainstPlayer = true;
            }
            m_actionDoneSignal.Reset();
            flash = MoveFlashing;
            UndoMove(isPlayerAgainstPlayer, flash);
            m_actionDoneSignal.Set();
        }

        /// <summary>
        /// Redo the most recently undone move
        /// </summary>
        /// <param name="isPlayerAgainstPlayer"> true if player against player</param>
        /// <param name="flash">                 true to flash while doing the move</param>
        /// <returns>
        /// NoRepeat, FiftyRuleRepeat, ThreeFoldRepeat, Check, Mate
        /// </returns>
        private ChessBoard.GameResult RedoMove(bool isPlayerAgainstPlayer, bool flash) {
            ChessBoard.GameResult retVal = ChessBoard.GameResult.OnGoing;
            MoveExt               move;
            int                   count;
            int                   redoCount;

            if (m_busyCount != 0) { 
                throw new MethodAccessException(m_ctlIsBusyMsg);
            }
            count     = isPlayerAgainstPlayer ? 1 : 2;
            redoCount = RedoCount;
            if (count > redoCount) {
                count = redoCount;
            }
            for (int i = 0; i < count; i++) {
                move   = m_board.MovePosStack.NextMove;
                ShowBeforeMove(move, flash);
                retVal = m_board.RedoMove();
                ShowAfterMove(move, flash);
                OnRedoPosChanged(EventArgs.Empty);
                OnUpdateCmdState(EventArgs.Empty);
                GameTimer.PlayerColor = m_board.CurrentPlayer;
                GameTimer.Enabled     = (retVal == ChessBoard.GameResult.OnGoing || retVal == ChessBoard.GameResult.Check);
            }
            return retVal;
        }

        /// <summary>
        /// Redo the most recently undone move
        /// </summary>
        /// <param name="isPlayerAgainstPlayer"> true if player against player</param>
        /// <returns>
        /// NoRepeat, FiftyRuleRepeat, ThreeFoldRepeat, Check, Mate
        /// </returns>
        public ChessBoard.GameResult RedoMove(bool isPlayerAgainstPlayer) {
            ChessBoard.GameResult retVal;
            bool                  flash;

            m_actionDoneSignal.Reset();
            flash  = MoveFlashing;
            retVal = RedoMove(isPlayerAgainstPlayer, flash);
            m_actionDoneSignal.Set();
            return retVal;
        }

        /// <summary>
        /// Select a move by index using undo/redo buffer to move
        /// </summary>
        /// <param name="index">   Index of the move. Can be -1</param>
        /// <param name="succeed"> true if index in range</param>
        /// <returns>
        /// Repeat result
        /// </returns>
        public ChessBoard.GameResult SelectMove(int index, out bool succeed) {
            ChessBoard.GameResult retVal = ChessBoard.GameResult.OnGoing;
            int                   curPos;
            int                   count;

            if (m_busyCount != 0) {
                throw new MethodAccessException(m_ctlIsBusyMsg);
            }
            m_actionDoneSignal.Reset();
            curPos = m_board.MovePosStack.PositionInList;
            count  = m_board.MovePosStack.Count;
            if (index >= -1 && index < count) {
                succeed = true;
                if (curPos < index) {
                    while (curPos != index) {
                        retVal = RedoMove(isPlayerAgainstPlayer: true ,  flash: false);
                        curPos++;
                    }
                } else if (curPos > index) {
                    while (curPos != index) {
                        UndoMove(isPlayerAgainstPlayer: true, flash: false);
                        curPos--;
                    }
                }
            } else {
                succeed = false;
            }
            m_actionDoneSignal.Set();
            return retVal;
        }

        /// <summary>
        /// Intercept Mouse click
        /// </summary>
        /// <param name="e">    Event Parameter</param>
        protected override void OnMouseDown(MouseButtonEventArgs e) {
            IntPoint                        pt;
            Move                            move;
            ChessBoard.ValidPawnPromotion   validPawnPromotion;
            QueryPieceEventArgs             queryPieceEventArgs;
            int                             pos;
            ChessBoard.PieceType            pieceType;
            QueryPawnPromotionTypeEventArgs eventArg;
            bool                            isWhiteToMove;
            bool                            isWhitePiece;
            
            base.OnMouseDown(e);
            if (m_busyCount == 0 && !IsSearchEngineBusy && !IsObservingAGame) {
                if (BoardDesignMode) {
                    if (GetCellFromPoint(e, out pt)) {
                        pos                 = pt.X + (pt.Y << 3);
                        queryPieceEventArgs = new QueryPieceEventArgs(pos, ChessBoard[pos]);
                        OnQueryPiece(queryPieceEventArgs);
                        ChessBoard[pos]     = queryPieceEventArgs.PieceType;
                        RefreshCell(pos);
                    }
                } else if (AutoSelection) {
                    if (GetCellFromPoint(e, out pt)) {
                        pos = pt.X + (pt.Y << 3);
                        if (SelectedCell.X == -1 || SelectedCell.Y == -1) {
                            pieceType     = m_board[pos];
                            isWhiteToMove = (m_board.CurrentPlayer == ChessBoard.PlayerColor.White);
                            isWhitePiece  = (pieceType & ChessBoard.PieceType.Black) == 0;
                            if (pieceType != ChessBoard.PieceType.None && isWhiteToMove == isWhitePiece) {
                                SelectedCell = pt;
                            } else {
                                Console.Beep();
                            }
                        } else {
                            if (SelectedCell.X == pt.X  && SelectedCell.Y == pt.Y) {
                                SelectedCell = new IntPoint(-1, -1);
                            } else {
                                move = ChessBoard.FindIfValid(m_board.CurrentPlayer,
                                                              SelectedCell.X + (SelectedCell.Y << 3),
                                                              pos);
                                if (move.StartPos != 255) {                                                           
                                    validPawnPromotion = ChessBoard.FindValidPawnPromotion(m_board.CurrentPlayer, 
                                                                                           SelectedCell.X + (SelectedCell.Y << 3),
                                                                                           pos);
                                    if (validPawnPromotion != ChessBoard.ValidPawnPromotion.None) {
                                        eventArg = new QueryPawnPromotionTypeEventArgs(validPawnPromotion);
                                        OnQueryPawnPromotionType(eventArg);
                                        if (eventArg.PawnPromotionType == Move.MoveType.Normal) {
                                            move.StartPos = 255;
                                        } else {
                                            move.Type &= ~Move.MoveType.MoveTypeMask;
                                            move.Type |= eventArg.PawnPromotionType;
                                        }
                                    }
                                }
                                SelectedCell = new IntPoint(-1, -1);
                                if (move.StartPos == 255) {
                                    Console.Beep();
                                } else {
                                    OnMoveSelected(new MoveSelectedEventArgs(new MoveExt(move)));
                                }
                            }
                        }
                    }
                }
            }
        }
        #endregion

    } // Class ChessBoardControl
} // Namespace
