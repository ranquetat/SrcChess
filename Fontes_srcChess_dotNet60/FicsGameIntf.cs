using System;
using System.Collections.Generic;
using System.Globalization;
using SrcChess2.Core;
using SrcChess2.PgnParsing;
using SrcChess2.FicsInterface;

namespace SrcChess2 {

    /// <summary>
    /// Interface between a Chess Server and a chess control board
    /// </summary>
    public class FicsGameIntf {
        /// <summary>Game being observed</summary>
        public  FicsGame                                          Game { get; }
        /// <summary>Chess board control</summary>
        public  ChessBoardControl                                 ChessBoardCtl { get; }
        /// <summary>true if board has already been created (move list has been received)</summary>
        public  bool                                              BoardCreated { get; private set; }
        /// <summary>Termination code</summary>
        public  TerminationCode                                   TerminationCode { get; private set; }
        /// <summary>Termination error</summary>
        public  string?                                           TerminationError { get; private set; }
        /// <summary>List of moves received before the game was created</summary>
        private readonly Queue<Style12MoveLine>                   m_moveQueue;
        /// <summary>Board used to convert move</summary>
        private readonly ChessBoard                               m_chessBoard;
        /// <summary>PGN parser</summary>
        private readonly PgnParser                                m_parser;
        /// <summary>PGN game</summary>
        private readonly PgnGame                                  m_pgnGame;
        /// <summary>Total time used by white player</summary>
        private TimeSpan                                          m_totalWhiteTime;
        /// <summary>Total time used by black player</summary>
        private TimeSpan                                          m_totalBlackTime;
        /// <summary>List of initial moves</summary>
        private readonly List<MoveExt>                            m_initialMoveList;
        /// <summary>Timer to handle move time out if any</summary>
        private readonly System.Threading.Timer?                  m_moveTimeoutTimer;
        /// <summary>Move time out in seconds</summary>
        private readonly int                                      m_moveTimeOut;
        /// <summary>Original maximum time allowed to both player</summary>
        private readonly TimeSpan?                                m_originalMaxTime;
        /// <summary>Action to call when the game is terminating</summary>
        private readonly Action<FicsGameIntf,TerminationCode,string>? m_gameFinishedAction;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="game">               FICS Game</param>
        /// <param name="chessBoardCtl">      Chess board control</param>
        /// <param name="moveTimeout">        Move timeout in second</param>
        /// <param name="moveTimeOutAction">  Action to call if move timeout</param>
        /// <param name="gameFinishedAction"> Action to do when game is finished</param>
        public FicsGameIntf(FicsGame                                     game,
                            ChessBoardControl                            chessBoardCtl,
                            int?                                         moveTimeout,
                            Action<FicsGameIntf>?                        moveTimeOutAction,
                            Action<FicsGameIntf,TerminationCode,string>? gameFinishedAction) {
            Game                 = game;
            ChessBoardCtl        = chessBoardCtl;
            BoardCreated         = false;
            m_chessBoard         = new ChessBoard(chessBoardCtl.Dispatcher);
            m_parser             = new PgnParser(m_chessBoard);
            m_moveQueue          = new Queue<Style12MoveLine>(16);
            m_totalWhiteTime     = TimeSpan.Zero;
            m_totalBlackTime     = TimeSpan.Zero;
            m_originalMaxTime    = (game.PlayerTimeInMin == 0) ? (TimeSpan?)null : TimeSpan.FromMinutes(game.PlayerTimeInMin);
            m_initialMoveList    = new List<MoveExt>(128);
            m_gameFinishedAction = gameFinishedAction;
            TerminationCode      = TerminationCode.None;
            m_moveTimeOut        = moveTimeout ?? int.MaxValue;
            m_pgnGame            = new PgnGame(createAttrList: true, createMoveList: true);
            m_pgnGame.Attrs!.Add("Event", $"FICS Game {game.GameId.ToString(CultureInfo.InvariantCulture)}");
            m_pgnGame.Attrs!.Add("Site", "FICS Server");
            m_pgnGame.Attrs!.Add("White", game.WhitePlayerName);
            m_pgnGame.Attrs!.Add("Black", game.BlackPlayerName);
            if (game.PlayerTimeInMin != 0 && chessBoardCtl != null) {
                chessBoardCtl.GameTimer.MaxWhitePlayTime = TimeSpan.FromMinutes(game.PlayerTimeInMin);
                chessBoardCtl.GameTimer.MaxBlackPlayTime = TimeSpan.FromMinutes(game.PlayerTimeInMin);
                chessBoardCtl.GameTimer.MoveIncInSec     = game.IncTimeInSec;
            }
            if (moveTimeout != 0 && moveTimeOutAction != null) {
                m_moveTimeoutTimer = new System.Threading.Timer(TimerCallback, moveTimeOutAction, System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
            }
        }

        /// <summary>
        /// Called when a timeout occurs
        /// </summary>
        /// <param name="state"></param>
        private void TimerCallback(object? state) {
            Action<FicsGameIntf> action;

            action = (Action<FicsGameIntf>)state!;
            if (TerminationCode == TerminationCode.None) {
                action(this);
            }
        }

        /// <summary>
        /// Send a message to the chess board control
        /// </summary>
        /// <param name="msg"> Message string</param>
        protected virtual void ShowMessage(string msg) => ChessBoardCtl.Dispatcher.Invoke((Action)(() => { ChessBoardCtl.ShowMessage(msg); }));

        /// <summary>
        /// Send an error to the chess board control
        /// </summary>
        /// <param name="errTxt"> Error string</param>
        public virtual void ShowError(string errTxt) => ChessBoardCtl.Dispatcher.Invoke((Action)(() => { ChessBoardCtl.ShowError(errTxt); }));

        /// <summary>
        /// Set the termination code and the error if any
        /// </summary>
        /// <param name="terminationCode">    Termination code</param>
        /// <param name="terminationComment"> Termination comment if any</param>
        /// <param name="errTxt">             Error if any</param>
        public virtual void SetTermination(TerminationCode terminationCode, string? terminationComment, string? errTxt) {
            string msg;

            if (m_moveTimeoutTimer != null) {
                m_moveTimeoutTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
            }
            TerminationCode = terminationCode;
            switch (terminationCode) {
            case TerminationCode.None:
                throw new ArgumentException("Cannot terminate with none");
            case TerminationCode.WhiteWin:
                msg = "White Win";
                break;
            case TerminationCode.BlackWin:
                msg = "Black Win";
                break;
            case TerminationCode.Draw:
                msg = "Draw";
                break;
            case TerminationCode.Terminated:
                msg = "Game finished";
                break;
            case TerminationCode.TerminatedWithErr:
                msg                = errTxt ?? "???";
                terminationComment = "";
                TerminationError   = errTxt;
                break;
            default:
                throw new ArgumentException("Invalid termination code", nameof(terminationCode));
            }
            if (!string.IsNullOrEmpty(terminationComment)) {
                msg += $" - {terminationComment}";
            }
            if (m_gameFinishedAction != null) {
                m_gameFinishedAction(this, terminationCode, msg);
            } else {
                if (terminationCode != TerminationCode.TerminatedWithErr) {
                    ShowMessage(msg);
                } else {
                    ShowError(msg);
                }
            }
        }

        /// <summary>
        /// Set the board content
        /// </summary>
        private void SetBoardControl() {
            void del() {
                ChessBoardCtl.CreateGameFromMove(startingChessBoard: null,
                                                 m_initialMoveList,
                                                 m_chessBoard.CurrentPlayer,
                                                 Game.WhitePlayerName,
                                                 Game.BlackPlayerName,
                                                 PgnPlayerType.Human,
                                                 PgnPlayerType.Human,
                                                 TimeSpan.Zero,
                                                 TimeSpan.Zero);
                ChessBoardCtl.GameTimer.ResetTo(Game.NextMovePlayer, Game.WhiteTimeSpan.Ticks, Game.BlackTimeSpan.Ticks);
                ChessBoardCtl.Refresh();
            }
            ChessBoardCtl.Dispatcher.Invoke(del);
        }

        /// <summary>
        /// Do a move
        /// </summary>
        /// <param name="move"> Move to be done</param>
        private void DoMove(MoveExt move) {
            int                    incrementTimeInSec;
            int                    moveCount;
            TimeSpan               span;
            ChessBoard.PlayerColor playerColor;

            lock(m_chessBoard) {
                playerColor = ChessBoardCtl.NextMoveColor;
                ChessBoardCtl.DoMove(move);
                if (m_originalMaxTime.HasValue && Game.IncTimeInSec != 0) {
                    moveCount          = ChessBoardCtl.Board.MovePosStack.Count  + 1 / 2;
                    incrementTimeInSec = Game.IncTimeInSec * moveCount;
                    span               = m_originalMaxTime.Value + TimeSpan.FromSeconds(incrementTimeInSec);
                    if (playerColor == ChessBoard.PlayerColor.Black) {
                        ChessBoardCtl.GameTimer.MaxBlackPlayTime = span;
                    } else {
                        ChessBoardCtl.GameTimer.MaxWhitePlayTime = span;
                    }
                }
                if (!ChessBoardCtl.SignalActionDone.WaitOne(0)) {
                    ChessBoardCtl.SignalActionDone.WaitOne();
                }
            }
        }

        /// <summary>
        /// Play a decoded move
        /// </summary>
        /// <param name="moveLine">  Move line</param>
        /// <param name="showError"> True to send the error to the chess board control</param>
        /// <param name="errTxt">    Error if any</param>
        /// <returns>
        /// true if succeed, false if failed
        /// </returns>
        private bool PlayMove(Style12MoveLine moveLine, bool showError, out string? errTxt) {
            bool retVal;
            int  halfMoveCount;

            errTxt = null;
            if (BoardCreated) {
                if (m_moveTimeoutTimer != null) {
                    m_moveTimeoutTimer.Change(m_moveTimeOut * 1000, System.Threading.Timeout.Infinite);
                }
                halfMoveCount = m_chessBoard.MovePosStack.Count;
                if (moveLine.HalfMoveCount != halfMoveCount) {
                    if (moveLine.HalfMoveCount != halfMoveCount + 1) {
                        errTxt    = $"Unsynchronized move - {moveLine.LastMoveSan}";
                    } else {
                        if (!m_parser.ApplySanMoveToBoard(m_pgnGame, moveLine.LastMoveSan, out MoveExt move)) {
                            errTxt = $"Illegal move - {moveLine.LastMoveSan}";
                        } else {
                            switch(moveLine.NextMovePlayer) {
                            case ChessBoard.PlayerColor.Black:
                                m_totalWhiteTime += moveLine.LastMoveSpan;
                                break;
                            case ChessBoard.PlayerColor.White:
                                m_totalBlackTime += moveLine.LastMoveSpan;
                                break;
                            }
                            if (ChessBoardCtl != null) {
                                ChessBoardCtl.Dispatcher.Invoke((Action)(() => { DoMove(move);
                                                                                 ChessBoardCtl.GameTimer.ResetTo(moveLine.NextMovePlayer, m_totalWhiteTime.Ticks, m_totalBlackTime.Ticks);
                                                                               }));
                            }
                        }
                    }
                }
            } else {
                m_moveQueue.Enqueue(moveLine);
            }
            if (errTxt == null) {
                retVal = true;
            } else {
                if (showError) {
                    ShowError(errTxt);
                }
                retVal = false;
            }
            return retVal;
        }

        /// <summary>
        /// Play a decoded move
        /// </summary>
        /// <param name="moveLine"> Move line</param>
        /// <returns>
        /// true if succeed, false if failed
        /// </returns>
        public bool PlayMove(Style12MoveLine moveLine) => PlayMove(moveLine, showError: true, out string? _);

        /// <summary>
        /// Convert the board to a PGN game
        /// </summary>
        /// <param name="chessBoard">   Chess board</param>
        /// <returns>
        /// </returns>
        private string GetPGNGame(ChessBoard chessBoard)
            => PgnUtil.GetPgnFromBoard(chessBoard,
                                       includeRedoMove: false,
                                       m_pgnGame.Event ?? "???",
                                       "FICS Server",
                                       DateTime.Now.ToString(CultureInfo.InvariantCulture),
                                       round: "1",
                                       m_pgnGame.WhitePlayerName ?? "???",
                                       m_pgnGame.BlackPlayerName ?? "???",
                                       PgnPlayerType.Human,
                                       PgnPlayerType.Human,
                                       m_pgnGame.WhiteSpan,
                                       m_pgnGame.BlackSpan);

        /// <summary>
        /// Gets the game in PGN format
        /// </summary>
        /// <returns>
        /// PGN formatted string
        /// </returns>
        public string GetPGNGame() => GetPGNGame((ChessBoardCtl == null) ? m_chessBoard : ChessBoardCtl.Board);

        /// <summary>
        /// Parse the initial move
        /// </summary>
        /// <param name="Line">     Line to parse</param>
        /// <param name="errTxt">   Returned error if any</param>
        /// <returns>
        /// true if succeed, false if error, null if all starting moves has been found
        /// </returns>
        public bool? ParseInitialMove(string Line, out string? errTxt) {
            bool? retVal;
            int   halfMoveIndex;
            int   moveIndex;

            halfMoveIndex = m_chessBoard.MovePosStack.Count;
            moveIndex     = halfMoveIndex / 2 + 1;
            retVal        = FicsGame.ParseMoveLine(moveIndex,
                                                   Line,
                                                   out string   whiteMove,
                                                   out TimeSpan whiteTime,
                                                   out string   blackMove,
                                                   out TimeSpan blackTime,
                                                   out errTxt);
            if (retVal == true) {
                if (!string.IsNullOrEmpty(whiteMove)) {
                    if (!m_parser.ApplySanMoveToBoard(m_pgnGame, whiteMove, out MoveExt move)) {
                        retVal = false;
                        errTxt = $"Illegal move - {whiteMove}";
                    } else {
                        m_initialMoveList.Add(move);
                        if (!string.IsNullOrEmpty(blackMove)) {
                            if (!m_parser.ApplySanMoveToBoard(m_pgnGame, blackMove, out move)) {
                                retVal = false;
                                errTxt = $"Illegal move - {blackMove}";
                            } else {
                                m_initialMoveList.Add(move);
                            }
                        }
                    }
                }
                if (retVal == true) {
                    m_totalWhiteTime += whiteTime;
                    m_totalBlackTime += blackTime;
                }
            }
            if (retVal == false && errTxt != null) {
                ShowError(errTxt);
            }
            return (retVal);
        }

        /// <summary>
        /// Create the initial board
        /// </summary>
        /// <returns>
        /// true if succeed, false if error, null if all starting moves has been found
        /// </returns>
        public bool CreateInitialBoard(out string? errTxt) {
            bool            retVal = true;
            int             halfMoveIndex;
            Style12MoveLine moveLine;

            halfMoveIndex  = m_chessBoard.MovePosStack.Count;
            while (m_moveQueue.Count != 0 && m_moveQueue.Peek().HalfMoveCount < halfMoveIndex) {
                m_moveQueue.Dequeue();
            }
            SetBoardControl();
            BoardCreated = true;
            errTxt       = null;
            if (m_moveQueue.Count != 0) {
                if (m_moveQueue.Peek().HalfMoveCount == halfMoveIndex) {
                    m_moveQueue.Dequeue();
                    while (m_moveQueue.Count != 0 && retVal) {
                        moveLine = m_moveQueue.Dequeue();
                        if (!PlayMove(moveLine)) {
                            retVal = false;
                        }
                    }
                } else {
                    errTxt = "Desynchronization between game and move";
                    retVal = false;
                }
            }
            return (retVal);
        }
    }
}
