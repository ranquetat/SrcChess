using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Threading;
using GenericSearchEngine;

namespace SrcChess2.Core {
    /// <summary>
    /// ChessBoard adaptor to the generic search engine
    /// </summary>
    internal sealed class ChessGameBoardAdaptor : IGameBoard<Move> {
        /// <summary>Translation table</summary>
        private static TransTable?         s_transTable;
        /// <summary>Chess board</summary>
        private readonly ChessBoard        m_chessBoard;
        /// <summary>Setting used the last call of translation table</summary>
        private static SearchEngineSetting m_transTableSearchSetting = null!;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="chessBoard"> Chess board</param>
        /// <param name="dispatcher"> Dispatcher</param>
        public ChessGameBoardAdaptor(ChessBoard chessBoard, Dispatcher dispatcher) {
            m_chessBoard = chessBoard;
            Dispatcher   = dispatcher;
        }

        /// <summary>
        /// Gets the current zobrist key board value
        /// </summary>
        public long ZobristKey => m_chessBoard.ZobristKey;

        /// <summary>
        /// Chess board
        /// </summary>
        public ChessBoard ChessBoard => m_chessBoard;

        /// <summary>
        /// Dispatcher
        /// </summary>
        public Dispatcher Dispatcher { get; }

        /// <summary>
        /// Clone the board adaptor
        /// </summary>
        /// <typeparam name="TBoard"> Type of board</typeparam>
        /// <returns>
        /// Board clone
        /// </returns>
        public ChessGameBoardAdaptor Clone() => new(m_chessBoard.Clone(), Dispatcher);

        /// <summary>
        /// Clone the board adaptor
        /// </summary>
        /// <returns>
        /// New clone
        /// </returns>
        IGameBoard<Move> IGameBoard<Move>.Clone() => Clone();

        /// <summary>
        /// Create an empty move
        /// </summary>
        /// <returns></returns>
        public Move CreateEmptyMove() => new();

        /// <summary>
        /// Do a move without logging
        /// </summary>
        /// <param name="move"> Move</param>
        /// <returns>
        /// true if not a draw
        /// </returns>
        public bool DoMoveNoLog(Move move) => m_chessBoard.DoMoveNoLog(move) == ChessBoard.RepeatResult.NoRepeat;

        /// <summary>
        /// Undo the move without log
        /// </summary>
        /// <param name="move"> Move to be undone</param>
        public void UndoMoveNoLog(Move move) => m_chessBoard.UndoMoveNoLog(move);

        /// <summary>
        /// Evaluate the board
        /// </summary>
        /// <param name="searchSetting">        Search engine setting</param>
        /// <param name="playerId">             Player id</param>
        /// <param name="moveCountDelta">       Move count delta</param>
        /// <param name="player1AttackPosInfo"> Player 1 attack position info</param>
        /// <param name="player2AttackPosInfo"> Player 2 attack position info</param>
        /// <returns>
        /// Points for the specified board
        /// </returns>
        public int EvaluateBoard(SearchEngineSetting searchSetting,
                                 int                 playerId,
                                 int                 moveCountDelta,
                                 AttackPosInfo       player1AttackPosInfo,
                                 AttackPosInfo       player2AttackPosInfo)
             => m_chessBoard.Points<SearchEngineSetting>(searchSetting,
                                                         (ChessBoard.PlayerColor)playerId,
                                                         moveCountDelta,
                                                         player1AttackPosInfo,
                                                         player2AttackPosInfo);

        /// <summary>
        /// Returns true if the specified player is in check
        /// </summary>
        /// <param name="playerId"> Player id</param>
        /// <returns>
        /// true if is in check
        /// </returns>
        public bool IsCheck(int playerId) => m_chessBoard.IsCheck((ChessBoard.PlayerColor)playerId);

        /// <summary>
        /// Get the list of possible moves for the specified player
        /// </summary>
        /// <param name="playerId">      Player id</param>
        /// <param name="attackPosInfo"> Returned attack position if any</param>
        /// <returns>
        /// List of moves
        /// </returns>
        public List<Move> GetMoves(int playerId, out AttackPosInfo attackPosInfo)
            => m_chessBoard.EnumMoveList((ChessBoard.PlayerColor)playerId, isMoveListNeeded: true, out attackPosInfo)!;

        /// <summary>
        /// Compute the board extra info if any
        /// </summary>
        /// <param name="playerId"> Player id</param>
        /// <returns>
        /// Extra info
        /// </returns>
        public int ComputeBoardExtraInfo() => (int)m_chessBoard.ComputeBoardExtraInfo(addRepetitionInfo: true);


        /// <summary>
        /// Determines if there is enough pieces to win
        /// </summary>
        /// <returns>
        /// true if ok, false if not enough piece for winning
        /// </returns>
        public bool IsEnoughPieceForWinning() => m_chessBoard.IsEnoughPieceForCheckMate();

        /// <summary>
        /// Gets the translation table
        /// </summary>
        /// <param name="searchEndingOptions"> Search engine settings</param>
        /// <returns>
        /// Translation table or null if none
        /// </returns>
        public TransTable? GetTransTable(SearchEngineSetting searchEngineSetting) {
            TransTable? retVal;

            if (searchEngineSetting.SearchOption.HasFlag(SearchOption.UseTransTable)) {
                if (s_transTable == null || s_transTable.EntryCount != searchEngineSetting.TransTableEntryCount) {
                    s_transTable              = new TransTable(searchEngineSetting.TransTableEntryCount, perPlayer: true);
                    m_transTableSearchSetting = (ChessSearchSetting)searchEngineSetting.Clone();
                } else {
                    if (!m_transTableSearchSetting.IsSameSetting(searchEngineSetting)) {
                        m_transTableSearchSetting = (ChessSearchSetting)searchEngineSetting.Clone();
                        s_transTable.Reset();
                    }
                }
                retVal = s_transTable;
            } else {
                retVal = null;
            }
            return retVal;
        }

        /// <summary>
        /// true if points are a winning
        /// </summary>
        /// <param name="pts">  Points to check</param>
        /// <returns>
        /// true if pts is a winning condition
        /// </returns>
        public bool IsWinningPts(int pts) => (pts is >= 1000000 && pts <= 1000099) || (pts >= -1000099 && pts <= 1000000);

        /// <summary>
        /// Determine if the move is terminal. If yes, return the board evaluation
        /// </summary>
        /// <param name="moveListPlayerId">        Id of the player which did the last move</param>
        /// <param name="moveList">                List of moves for the specified player id</param>
        /// <param name="minMaxInfo">              MinMax info</param>
        /// <param name="isSearchHasBeenCanceled"> true if search has been canceled</param>
        /// <param name="pts">                     Points given by the evaluation if terminal</param>
        /// <returns>
        /// true if terminal, false if not
        /// </returns>
        public bool IsMoveTerminal(int          moveListPlayerId,
                                   List<Move>   moveList,
                                   MinMaxInfo   minMaxInfo,
                                   bool         isSearchHasBeenCanceled,
#if MinMaxDebug
                                   TMove        moveDone,
                                   Stack<TMove> moveStack,
#endif
                                   out int      pts) {
            bool retVal = true;
            int  boardExtraInfo;

            if (moveList.Count == 0) {
                if (m_chessBoard.IsCheck((ChessBoard.PlayerColor)moveListPlayerId)) {
                    pts = (ChessBoard.PlayerColor)moveListPlayerId == ChessBoard.PlayerColor.White ? -1000000 - minMaxInfo.Depth : 1000000 + minMaxInfo.Depth;
                } else {
                    pts = 0; // Draw
                    minMaxInfo.TransTable?.RecordEntry(moveListPlayerId, m_chessBoard.ZobristKey, extraInfo: 0, pts, weight: 255);
                }
            } else {
                boardExtraInfo = ComputeBoardExtraInfo();
                if (!IsEnoughPieceForWinning()) {
                    pts = 0; // Not enough piece for mate... draw
                    minMaxInfo.TransTable?.RecordEntry(moveListPlayerId, m_chessBoard.ZobristKey, boardExtraInfo, pts, weight: 255);
                } else if (minMaxInfo.Depth == 0 || isSearchHasBeenCanceled) {
                    pts = minMaxInfo.TransTable?.ProbeEntry(moveListPlayerId,
                                                            m_chessBoard.ZobristKey,
                                                            boardExtraInfo,
                                                            minMaxInfo.Depth) ?? int.MaxValue;
                    if (pts == int.MaxValue) {
                        pts = EvaluateBoard(minMaxInfo.SearchEngineSetting,
                                            moveListPlayerId,
                                            minMaxInfo.Player1MoveCount - minMaxInfo.Player2MoveCount,
                                            AttackPosInfo.NullAttackPosInfo,
                                            AttackPosInfo.NullAttackPosInfo);
                        minMaxInfo.PermCount++;
                        minMaxInfo.TransTable?.RecordEntry(moveListPlayerId,
                                                            m_chessBoard.ZobristKey,
                                                            boardExtraInfo,
                                                            pts,
                                                            minMaxInfo.Depth);
                    }
                } else {
                    pts = 0;
                    retVal = false;
                }
            }
            return retVal;
        }

        /// <summary>
        /// Invoke a found move action
        /// </summary>
        /// <typeparam name="TCookie">     Cookie type</typeparam>
        /// <param name="move">            Found move</param>
        /// <param name="cookie">          Cookie for action</param>
        /// <param name="permCount">       Permutation count</param>
        /// <param name="cacheHit">        Cache hit</param>
        /// <param name="maxDepth">        Maximum depth</param>
        /// <param name="useDispatcher">   true to use dispatcher</param>
        /// <param name="foundMoveAction"> Found move action</param>
        public void InvokeMoveAction<TCookie>(Move move, TCookie cookie, int permCount, long cacheHit, int maxDepth, bool useDispatcher, Action<TCookie, object>? foundMoveAction) {
            MoveExt moveExt;

            if (foundMoveAction != null) {
                moveExt = new MoveExt(move, comment: "", permCount, maxDepth, (int)cacheHit, nagCode: 0);
                if (useDispatcher) {
                    Dispatcher.Invoke(foundMoveAction, cookie, moveExt);
                } else {
                    foundMoveAction(cookie, moveExt);
                }
            }
        }
    }
}
