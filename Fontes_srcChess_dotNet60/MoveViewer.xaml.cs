using System;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using SrcChess2.Core;

namespace SrcChess2 {
    /// <summary>
    /// Move Item
    /// </summary>
    public class MoveItem {
        /// <summary>Step</summary>
        public  string  Step { get; set; }
        /// <summary>Who did the move</summary>
        public  string  Who { get; set; }
        /// <summary>Move</summary>
        public  string  Move { get; set; }

        /// <summary>
        /// Class Ctor
        /// </summary>
        /// <param name="step"> Move step</param>
        /// <param name="who">  Who did the move</param>
        /// <param name="move"> Move</param>
        public MoveItem(string step, string who, string move) {
            Step = step;
            Who  = who;
            Move = move; 
        }
    }

    /// <summary>List of moves</summary>
    public class MoveItemList : ObservableCollection<MoveItem> {}

    /// <summary>
    /// User interface displaying the list of moves
    /// </summary>
    public partial class MoveViewer : UserControl {

        /// <summary>How the move are displayed: Move position (E2-E4) or PGN (e4)</summary>
        public enum ViewerDisplayMode {
            /// <summary>Display move using starting-ending position</summary>
            MovePos,
            /// <summary>Use PGN notation</summary>
            Pgn
        }
        
        /// <summary>Argument for the NewMoveSelected event</summary>
        public class NewMoveSelectedEventArg : System.ComponentModel.CancelEventArgs {
            /// <summary>New selected index in the list</summary>
            public int NewIndex { get; set; }

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="newIndex">    New index</param>
            public NewMoveSelectedEventArg(int newIndex) : base(false) => NewIndex = newIndex;

        }
        
        /// <summary>Called when a move has been selected by the control</summary>
        public event EventHandler<NewMoveSelectedEventArg>? NewMoveSelected;
        /// <summary>Chess board control associated with the move viewer</summary>
        private ChessBoardControl?                          m_chessCtl;
        /// <summary>Display Mode</summary>
        private ViewerDisplayMode                           m_displayMode;
        /// <summary>true to ignore change</summary>
        private bool                                        m_ignoreChg;
        /// <summary>List of moves</summary>
        public  MoveItemList                                MoveList { get; }

        /// <summary>
        /// Class Ctor
        /// </summary>
        public MoveViewer() {
            InitializeComponent();
            m_displayMode                      = ViewerDisplayMode.MovePos;
            m_ignoreChg                        = false;
            MoveList                           = (MoveItemList)listViewMoveList.ItemsSource;
            listViewMoveList.SelectionChanged += new SelectionChangedEventHandler(ListViewMoveList_SelectionChanged);
        }

        /// <summary>
        /// Chess board control associated with move viewer
        /// </summary>
        public ChessBoardControl? ChessControl {
            get => m_chessCtl;
            set {
                if (m_chessCtl != value) {
                    if (m_chessCtl != null) { 
                        m_chessCtl.BoardReset     -= ChessCtl_BoardReset;
                        m_chessCtl.NewMove        -= ChessCtl_NewMove;
                        m_chessCtl.RedoPosChanged -= ChessCtl_RedoPosChanged;
                    }
                    m_chessCtl = value;
                    if (m_chessCtl != null) { 
                        m_chessCtl.BoardReset     += ChessCtl_BoardReset;
                        m_chessCtl.NewMove        += ChessCtl_NewMove;
                        m_chessCtl.RedoPosChanged += ChessCtl_RedoPosChanged;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the description of a move
        /// </summary>
        /// <param name="move"> Move to describe</param>
        /// <returns>
        /// Move description
        /// </returns>
        private string GetMoveDesc(MoveExt move) {
            string  retVal;
            
            if (m_displayMode == ViewerDisplayMode.MovePos) {
                retVal = move.GetHumanPos();
            } else {
                retVal = PgnUtil.GetPgnMoveFromMove(m_chessCtl!.Board, move, includeEnding: false);
                if ((move.Move.Type & Move.MoveType.MoveFromBook) == Move.MoveType.MoveFromBook) {
                    retVal = $"({retVal})";
                }
            }
            return retVal;
        }

        /// <summary>
        /// Redisplay all the moves using the current setting
        /// </summary>
        private void Redisplay() {
            string[]?    moveNames;
            int          moveCount;
            MovePosStack movePosStack;
            MoveExt      move;
            string       moveTxt;
            string       moveIndex;
            MoveItem     moveItem;
            ChessBoard   chessBoard;

            chessBoard = m_chessCtl!.Board;
            if (chessBoard != null) {
                movePosStack = chessBoard.MovePosStack;
                moveCount    = movePosStack.Count;
                if (moveCount != 0) {
                    if (m_displayMode == ViewerDisplayMode.MovePos) {
                        moveNames = null;
                    } else {
                        moveNames = PgnUtil.GetPgnArrayFromMoveList(chessBoard);
                    }
                    for (int i = 0; i < moveCount; i++) {
                        move = movePosStack[i];
                        if (m_displayMode == ViewerDisplayMode.MovePos) {
                            moveTxt   = move.GetHumanPos();
                            moveIndex = (i + 1).ToString();
                        } else {
                            moveTxt   = moveNames![i];
                            moveIndex = (i / 2 + 1).ToString() + ((Char)('a' + (i & 1))).ToString();
                        }
                        moveItem      = MoveList![i];
                        MoveList[i]   = new MoveItem(moveIndex, moveItem.Who, moveTxt);
                    }
                }
            }
        }

        /// <summary>
        /// Add the current move of the board
        /// </summary>
        private void AddCurrentMove() {
            MoveItem               moveItem;
            string                 moveTxt;
            string                 moveIndex;
            int                    moveCount;
            int                    itemCount;
            int                    index;
            MoveExt                move;
            ChessBoard.PlayerColor playerToMove;
            ChessBoard             chessBoard;

            chessBoard   = m_chessCtl!.Board;            
            m_ignoreChg  = true;
            move         = chessBoard.MovePosStack.CurrentMove;
            playerToMove = chessBoard.LastMovePlayer;
            chessBoard.UndoMove();
            moveCount    = chessBoard.MovePosStack.Count;
            itemCount    = listViewMoveList.Items.Count;
            while (itemCount >= moveCount) {
                itemCount--;
                MoveList.RemoveAt(itemCount);
            }
            moveTxt = GetMoveDesc(move);
            chessBoard.RedoMove();
            index     = itemCount;
            moveIndex = (m_displayMode == ViewerDisplayMode.MovePos) ? (index + 1).ToString() : (index / 2 + 1).ToString() + ((Char)('a' + (index & 1))).ToString();
            moveItem  = new MoveItem(moveIndex,
                                     (playerToMove == ChessBoard.PlayerColor.Black) ? "Black" : "White",
                                     moveTxt);
            MoveList.Add(moveItem);
            m_ignoreChg = false;
        }

        /// <summary>
        /// Select the current move
        /// </summary>
        private void SelectCurrentMove() {
            int        index;
            MoveItem   moveItem;
            ChessBoard chessBoard;

            chessBoard  = m_chessCtl!.Board;
            m_ignoreChg = true;
            index       = chessBoard.MovePosStack.PositionInList;
            if (index == -1) {
                listViewMoveList.SelectedItem = null;
            } else {
                moveItem                      = (MoveItem)listViewMoveList.Items[index];
                listViewMoveList.SelectedItem = moveItem;
                listViewMoveList.ScrollIntoView(moveItem);
            }
            m_ignoreChg = false;
        }

        /// <summary>
        /// Display Mode (Position or PGN)
        /// </summary>
        public ViewerDisplayMode DisplayMode {
            get => m_displayMode;
            set {
                if (value != m_displayMode) {
                    m_displayMode = value;
                    Redisplay();
                }
            }
        }

        /// <summary>
        /// Reset the control so it represents the specified chessboard
        /// </summary>
        private void Reset() {
            int         count;
            ChessBoard  chessBoard;
            
            MoveList.Clear();
            chessBoard = m_chessCtl!.Board;
            count      = chessBoard.MovePosStack.Count;
            chessBoard.UndoAllMoves();
            for (int i = 0; i < count; i++) {
                chessBoard.RedoMove();
                AddCurrentMove();
            }
            SelectCurrentMove();
        }

        /// <summary>
        /// Trigger the NewMoveSelected argument
        /// </summary>
        /// <param name="e"> Event arguments</param>
        protected void OnNewMoveSelected(NewMoveSelectedEventArg e) => NewMoveSelected?.Invoke(this, e);

        /// <summary>
        /// Triggered when the position has been changed
        /// </summary>
        /// <param name="sender"> Sender object</param>
        /// <param name="e">      Event arguments</param>
        private void ChessCtl_RedoPosChanged(object? sender, EventArgs e) => SelectCurrentMove();

        /// <summary>
        /// Triggered when a new move has been done
        /// </summary>
        /// <param name="sender"> Sender object</param>
        /// <param name="e">      Event arguments</param>
        private void ChessCtl_NewMove(object? sender, ChessBoardControl.NewMoveEventArgs e) {
            AddCurrentMove();
            SelectCurrentMove();
        }


        /// <summary>
        /// Triggered when the board has been reset
        /// </summary>
        /// <param name="sender"> Sender object</param>
        /// <param name="e">      Event arguments</param>
        private void ChessCtl_BoardReset(object? sender, EventArgs e) => Reset();

        /// <summary>
        /// Called when the user select a move
        /// </summary>
        /// <param name="sender"> Sender object</param>
        /// <param name="e">      Event arguments</param>
        private void ListViewMoveList_SelectionChanged(object? sender, SelectionChangedEventArgs e) {
            NewMoveSelectedEventArg evArg;
            int                     curPos;
            int                     newPos;
            ChessBoard              chessBoard;
            
            if (!m_ignoreChg && !m_chessCtl!.IsBusy && !ChessBoardControl.IsSearchEngineBusy) {
                m_ignoreChg = true;
                chessBoard  = m_chessCtl!.Board;
                curPos      = chessBoard.MovePosStack.PositionInList;
                if (e.AddedItems.Count != 0) {
                    newPos = listViewMoveList.SelectedIndex;
                    if (newPos != curPos) {
                        evArg = new NewMoveSelectedEventArg(newPos);
                        OnNewMoveSelected(evArg);
                        if (evArg.Cancel) {
                            if (curPos == -1) {
                                listViewMoveList.SelectedItems.Clear();
                            } else {
                                listViewMoveList.SelectedIndex  = curPos;
                            }
                        }
                    }
                }
                m_ignoreChg = false;
            }
        }
    } // Class MoveViewer
} // Namespace
