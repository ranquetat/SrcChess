using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GenericSearchEngine {

    /// <summary>
    /// Alpha Beta search engine
    /// </summary>
    public sealed class SearchEngineAlphaBeta<TBoard, TMove> : SearchEngine<TBoard, TMove>
                                                               where TMove  : struct
                                                               where TBoard : IGameBoard<TMove> {

        /// <summary>
        /// Class constructor
        /// </summary>
        /// <param name="trace">  Trace object or null</param>
        /// <param name="rnd">    Random object</param>
        /// <param name="rndRep"> Repetitive random object</param>
        public SearchEngineAlphaBeta(ISearchTrace<TMove>? trace, Random rnd, Random rndRep) : base(trace, rnd, rndRep) {}

        /// <summay>
        /// Alpha Beta pruning function.
        /// </summary>
        /// <param name="moveListPlayerId">   Player doing the move</param>
        /// <param name="board">              Chess board</param>
        /// <param name="moveList">           Move list</param>
        /// <param name="alpha">              Alpha limit</param>
        /// <param name="beta">               Beta limit</param>
        /// <param name="minMaxInfo">         MinMax information</param>
        /// <param name="bestMovePos">        Best move position or -1 if none found</param>
        /// <param name="ptsPerMove">         Points per move if not null</param>
        /// <param name="evaluatedMoveCount"> Number of moves evaluated</param>
        /// <param name="isFullyEvaluated">   true if all moves has been evaluated</param>
        /// <param name="maximizing">         Maximizing</param>
        /// <returns>
        /// Points to give for this move or int.MinValue for timed out
        /// </returns>
        private static int AlphaBeta(int            moveListPlayerId,
                                     TBoard         board,
                                     List<TMove>    moveList,
                                     int            alpha,
                                     int            beta,
                                     ref MinMaxInfo minMaxInfo,
                                     out int        bestMovePos,
                                     int[]?         ptsPerMove,
                                     out int        evaluatedMoveCount,
                                     out bool       isFullyEvaluated,
                                     bool           maximizing) { 
            int         value;
            int         lastMovePlayerId;
            List<TMove> childMoveList;
            int         boardExtraInfo;

            evaluatedMoveCount     = 0;
            bestMovePos            = -1;
            lastMovePlayerId       = 1 - moveListPlayerId;
            minMaxInfo.HasTimedOut = DateTime.Now >= minMaxInfo.TimeOut;
            isFullyEvaluated       = true;
            if (!board.IsMoveTerminal(moveListPlayerId,
                                      moveList,
                                      minMaxInfo,
                                      isSearchHasBeenCanceled: DateTime.Now >= minMaxInfo.TimeOut || IsSearchHasBeenCanceled,
                                      out int retVal)) {
                retVal = maximizing ? int.MinValue : int.MaxValue;
                foreach (TMove move in moveList) {
                    if (board.DoMoveNoLog(move)) {
                        boardExtraInfo = board.ComputeBoardExtraInfo();
                        value          = minMaxInfo.TransTable?.ProbeEntry(moveListPlayerId, board.ZobristKey, boardExtraInfo, minMaxInfo.Depth - 1) ?? int.MaxValue;
                        if (value == int.MaxValue) {
                            childMoveList = board.GetMoves(lastMovePlayerId, out AttackPosInfo _);
                            minMaxInfo.Depth--;
                            value = AlphaBeta(lastMovePlayerId,
                                              board,
                                              childMoveList,
                                              alpha,
                                              beta,
                                              ref minMaxInfo,
                                              bestMovePos: out int _,
                                              ptsPerMove:  null,
                                              evaluatedMoveCount: out int _,
                                              out bool isChildIsFullyEvaluated,
                                              !maximizing);
                            minMaxInfo.Depth++;
                            if (board.IsWinningPts(value) && isChildIsFullyEvaluated) { // Can only be if this is coming directly from Terminal
                                isChildIsFullyEvaluated = false;
                            }
                            if (isChildIsFullyEvaluated) {
                                minMaxInfo.TransTable?.RecordEntry(moveListPlayerId, board.ZobristKey, boardExtraInfo, value, minMaxInfo.Depth);
                            }
                            isFullyEvaluated &= isChildIsFullyEvaluated;
                        }
                        board.UndoMoveNoLog(move);
                    } else {
                        value = 0; // draw
                        board.UndoMoveNoLog(move);
                    }
                    if (ptsPerMove != null) {
                        ptsPerMove[evaluatedMoveCount] = value;
                    }
                    if (maximizing) {
                        if (value > retVal) {
                            retVal      = value;
                            bestMovePos = evaluatedMoveCount;
                            if (retVal >= beta) {
                                evaluatedMoveCount++;
                                if (evaluatedMoveCount < moveList.Count) {
                                    isFullyEvaluated = false;
                                }
                                break;
                            }
                            alpha = Math.Max(alpha, retVal);
                        }
                    } else {
                        if (value < retVal) {
                            retVal      = value;
                            bestMovePos = evaluatedMoveCount;
                            if (value <= alpha) {
                                evaluatedMoveCount++;
                                if (evaluatedMoveCount < moveList.Count) {
                                    isFullyEvaluated = false;
                                }
                                break;
                            }
                            beta = Math.Min(beta, retVal);
                        }
                    }
                    evaluatedMoveCount++;
                }
            }
            return retVal;
        }

        /// <summary>
        /// Find the best move for a player using alpha-beta for a given depth
        /// </summary>
        /// <param name="board">               Chess board</param>
        /// <param name="searchEngineSetting"> Search mode</param>
        /// <param name="transTable">          Transposition table or null if not using one</param>
        /// <param name="playerId">            Color doing the move</param>
        /// <param name="moveList">            List of move to try</param>
        /// <param name="player1PosInfo">      Information about pieces attacks for the white</param>
        /// <param name="player2PosInfo">      Information about pieces attacks for the black</param>
        /// <param name="maxDepth">            Maximum depth</param>
        /// <param name="totalMoveCount">      Total list of moves</param>
        /// <param name="alpha">               Alpha bound</param>
        /// <param name="beta">                Beta bound</param>
        /// <param name="timeOut">             Time limit (DateTime.MaxValue for no time limit)</param>
        /// <param name="maximizing">          true for maximizing, false for minimizing</param>
        /// <param name="ptsPerMove">          Points for each move in the MoveList</param>
        /// <param name="evaluatedMoveCount">  Number of moves evaluated</param>
        /// <param name="bestMove">            Index of the best move</param>
        /// <param name="hasTimedOut">         Return true if time out</param>
        /// <param name="bestMovePts">         Return the best move point</param>
        /// <param name="permCount">           Total permutation evaluated</param>
        /// <returns>
        /// true if a best move has been found
        /// </returns>
        private bool FindBestMoveUsingAlphaBetaAtDepth(TBoard              board,
                                                       SearchEngineSetting searchEngineSetting,
                                                       TransTable?         transTable,
                                                       int                 playerId,
                                                       List<TMove>         moveList,
                                                       AttackPosInfo       player1PosInfo,
                                                       AttackPosInfo       player2PosInfo,
                                                       int                 maxDepth,
                                                       int                 totalMoveCount,
                                                       int                 alpha,
                                                       int                 beta,
                                                       DateTime            timeOut,
                                                       bool                maximizing,
                                                       int[]?              ptsPerMove,
                                                       out int             evaluatedMoveCount,
                                                       out TMove           bestMove,
                                                       out bool            hasTimedOut,
                                                       out int             bestMovePts,
                                                       out int             permCount) {
            bool       retVal = false;
            MinMaxInfo minMaxInfo;
            int        player1MoveCount;
            int        player2MoveCount;

            if (playerId == PlayerId1) {
                player1MoveCount = totalMoveCount;
                player2MoveCount = 0;
            } else {
                player1MoveCount = 0;
                player2MoveCount = totalMoveCount;
            }
            minMaxInfo = new MinMaxInfo(searchEngineSetting, transTable) {
                PermCount            = 0,
                Depth                = maxDepth,
                MaxDepth             = maxDepth,
                TimeOut              = timeOut,
                HasTimedOut          = false,
                Player1AttackPosInfo = player1PosInfo,
                Player2AttackPosInfo = player2PosInfo,
                Player1MoveCount     = player1MoveCount,
                Player2MoveCount     = player2MoveCount
            };
            bestMovePts = AlphaBeta(playerId,
                                    board,
                                    moveList,
                                    alpha,
                                    beta,
                                    ref minMaxInfo,
                                    out int bestMovePos,
                                    ptsPerMove,
                                    out evaluatedMoveCount,
                                    isFullyEvaluated: out bool _,
                                    maximizing);
            if (bestMovePos !=  -1) {
                bestMove = moveList[bestMovePos];
                retVal   = true;
                LogSearchTrace(maxDepth, playerId, bestMove, bestMovePts);
            } else {
                bestMove = board.CreateEmptyMove();
            }
            permCount   = minMaxInfo.PermCount;
            hasTimedOut = minMaxInfo.HasTimedOut;
            return retVal;
        }

        /// <summary>
        /// Find the best move for a player using alpha-beta pruning. 
        /// One or more thread can execute this method at the same time.
        /// Handle the search method:
        ///     Fix depth,
        ///     Iterative fix depth 
        ///     Iterative depth with time limit
        /// </summary>
        /// <param name="board">               Chess board</param>
        /// <param name="searchEngineSetting"> Search mode</param>
        /// <param name="transTable">          Translation table if any</param>
        /// <param name="playerId">            Color doing the move</param>
        /// <param name="moveList">            List of move to try</param>
        /// <param name="player1PosInfo">      Information about pieces attacks for the white</param>
        /// <param name="player2PosInfo">      Information about pieces attacks for the black</param>
        /// <param name="totalMoveCount">      Total number of moves</param>
        /// <param name="alpha">               Alpha bound</param>
        /// <param name="beta">                Beta bound</param>
        /// <param name="maximizing">          true for maximizing, false for minimizing</param>
        /// <returns>
        /// Points
        /// </returns>
        private MinMaxResult<TMove> FindBestMoveUsingAlphaBetaAsync(TBoard              board,
                                                                    SearchEngineSetting searchEngineSetting,
                                                                    TransTable?         transTable,
                                                                    int                 playerId,
                                                                    List<TMove>         moveList,
                                                                    AttackPosInfo       player1PosInfo,
                                                                    AttackPosInfo       player2PosInfo,
                                                                    int                 totalMoveCount,
                                                                    int                 alpha,
                                                                    int                 beta,
                                                                    bool                maximizing) {
            MinMaxResult<TMove> retVal;
#if IterativeActivated
            DateTime            timeOut;
            int                 maxDepth;
            int                 depthLimit;
            int[]               ptsPerMove;
            bool                hasTimedOut;
#endif
            ThreadPriority      threadPriority;
            bool                bestMoveFound;
            bool                isIterativeDepthFirst;

            threadPriority                = Thread.CurrentThread.Priority;
            Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
            isIterativeDepthFirst         = searchEngineSetting.SearchOption.HasFlag(SearchOption.UseIterativeDepthSearch);
            retVal = new MinMaxResult<TMove> {
                BestMoveFound = false,
                BestMove      = board.CreateEmptyMove()
            };
            try {
                retVal.PermCount = 0;
#if IterativeActivated
                if (searchEngineSetting.SearchDepth == 0 || isIterativeDepthFirst) {
                    // Iterative Depth (with limit time or fixed maximum iteration)
                    ptsPerMove    = new int[moveList.Count];
                    timeOut       = (isIterativeDepthFirst) ? DateTime.MaxValue : 
                                                              DateTime.Now + TimeSpan.FromSeconds(searchEngineSetting.TimeOutInSec);
                    depthLimit    = (isIterativeDepthFirst) ? searchEngineSetting.SearchDepth : 999;
                    maxDepth      = 1;
                    bestMoveFound = FindBestMoveUsingAlphaBetaAtDepth(board,
                                                                      searchEngineSetting,
                                                                      transTable,
                                                                      playerId,
                                                                      moveList,
                                                                      player1PosInfo,
                                                                      player2PosInfo,
                                                                      maxDepth,
                                                                      totalMoveCount,
                                                                      alpha,
                                                                      beta,
                                                                      timeOut: DateTime.MaxValue,
                                                                      maximizing,
                                                                      ptsPerMove,
                                                                      out int evaluatedMoveCount,
                                                                      out TMove bestMove,
                                                                      hasTimedOut: out bool _,
                                                                      out int pts,
                                                                      out int permCountAtLevel);
                    if (bestMoveFound) {
                        retVal.BestMoveFound = true;
                        retVal.BestMove      = bestMove;
                        retVal.Pts           = pts;
                        retVal.MaxDepth      = maxDepth;
                    }
                    retVal.PermCount += permCountAtLevel;
                    hasTimedOut       = false;
                    while (DateTime.Now < timeOut && !SearchCancelState && !hasTimedOut && maxDepth < depthLimit) {
                        moveList = SortMoveList(moveList, ptsPerMove, evaluatedMoveCount);
                        maxDepth++;
                        bestMoveFound = FindBestMoveUsingAlphaBetaAtDepth(board,
                                                                          searchEngineSetting,
                                                                          transTable,
                                                                          playerId,
                                                                          moveList,
                                                                          player1PosInfo,
                                                                          player2PosInfo,
                                                                          maxDepth,
                                                                          totalMoveCount,
                                                                          alpha,
                                                                          beta,
                                                                          timeOut,
                                                                          maximizing,
                                                                          ptsPerMove,
                                                                          out evaluatedMoveCount,
                                                                          out bestMove,
                                                                          out hasTimedOut,
                                                                          out pts,
                                                                          out permCountAtLevel);
                        if (bestMoveFound && !hasTimedOut) {
                            retVal.BestMoveFound = true;
                            retVal.BestMove      = bestMove;
                            retVal.Pts           = pts;
                            retVal.MaxDepth      = maxDepth;
                        }
                        retVal.PermCount += permCountAtLevel;
                    }
                } else {
#endif
                    // Fixed Maximum Depth
                    retVal.MaxDepth = searchEngineSetting.SearchDepth;
                    bestMoveFound   = FindBestMoveUsingAlphaBetaAtDepth(board,
                                                                        searchEngineSetting,
                                                                        transTable,
                                                                        playerId,
                                                                        moveList,
                                                                        player1PosInfo,
                                                                        player2PosInfo,
                                                                        retVal.MaxDepth,
                                                                        totalMoveCount,
                                                                        alpha,
                                                                        beta,
                                                                        DateTime.MaxValue,
                                                                        maximizing,
                                                                        ptsPerMove: null,
                                                                        evaluatedMoveCount: out int _,
                                                                        out TMove bestMove,
                                                                        hasTimedOut: out bool _,
                                                                        out int pts,
                                                                        out int permCountAtLevel);
                    if (bestMoveFound) {
                        retVal.BestMoveFound = true;
                        retVal.BestMove      = bestMove;
                        retVal.Pts           = pts;
                    }
                    retVal.PermCount += permCountAtLevel;
#if IterativeActivated
            }
#endif
            } finally {
                Thread.CurrentThread.Priority = threadPriority;
            }
            return (retVal);
        }

        /// <summary>
        /// Find the best move using Alpha Beta pruning.
        /// Handle the number of thread assignment for the search.
        /// </summary>
        /// <param name="board">               Board</param>
        /// <param name="searchEngineSetting"> Search mode</param>
        /// <param name="playerId">            Player doing the move</param>
        /// <param name="moveList">            Move list</param>
        /// <param name="attackPosInfo">       Attack/defense position info</param>
        /// <param name="bestMove">            Best move found</param>
        /// <param name="permCount">           Total permutation evaluated</param>
        /// <param name="cacheHit">            Number of moves found in the translation table cache</param>
        /// <param name="maxDepth">            Maximum depth used</param>
        /// <returns>
        /// true if a move has been found
        /// </returns>
        protected override bool FindBestMove(TBoard              board,
                                             SearchEngineSetting searchEngineSetting,
                                             int                 playerId,
                                             List<TMove>         moveList,
                                             AttackPosInfo       attackPosInfo,
                                             ref TMove           bestMove,
                                             out int             permCount,
                                             out long            cacheHit,
                                             out int             maxDepth) { 
            bool                        retVal = false;
            TBoard[]                    boards;
            TransTable?                 transTable;
            Task<MinMaxResult<TMove>>[] tasks;
            List<TMove>[]               taskMoveList;
            MinMaxResult<TMove>         minMaxRes;
            AttackPosInfo               player1PosInfo;
            AttackPosInfo               player2PosInfo;
            int                         threadCount;
            int                         moveListPos;
            int                         moveListCount;
            int                         movePerTask;
            int                         overflowCount;
            int                         pts;
            bool                        maximizing;

            if (playerId == PlayerId1) {
                player1PosInfo = attackPosInfo;
                player2PosInfo = AttackPosInfo.NullAttackPosInfo;
            } else {
                player1PosInfo = AttackPosInfo.NullAttackPosInfo;
                player2PosInfo = attackPosInfo;
            }
            maximizing    = playerId == PlayerId1;
            permCount     = 0;
            transTable    = board.GetTransTable(searchEngineSetting);
            transTable?.ResetCacheHit();
            moveListCount = moveList.Count;
            threadCount   = searchEngineSetting.ThreadingMode == ThreadingMode.OnePerProcessorForSearch ?
                            Math.Min(Environment.ProcessorCount, moveListCount) : 1;
            if (threadCount > 1) {
                boards        = new TBoard[threadCount];
                taskMoveList  = new List<TMove>[threadCount];
                tasks         = new Task<MinMaxResult<TMove>>[threadCount];
                moveListPos   = 0;
                movePerTask   = moveListCount / threadCount;
                overflowCount = moveListCount % threadCount;
                for (int i = 0; i < threadCount; i++) {
                    boards[i]       = (TBoard)board.Clone();
                    taskMoveList[i] = new List<TMove>(movePerTask);
                    for (int j = 0; j < movePerTask; j++) {
                        taskMoveList[i].Add(moveList[moveListPos++]);
                    }
                    if (overflowCount != 0) {
                        taskMoveList[i].Add(moveList[moveListPos++]);
                        overflowCount--;
                    }
                }
                for (int i = 0; i < threadCount; i++) {
                    tasks[i] = Task<MinMaxResult<TMove>>.Factory.StartNew((param) => {
                        int step = (int)param!;
                        return (FindBestMoveUsingAlphaBetaAsync(boards[step],
                                                                searchEngineSetting,
                                                                transTable,
                                                                playerId,
                                                                taskMoveList[step],
                                                                player1PosInfo,
                                                                player2PosInfo,
                                                                moveList.Count,
                                                                alpha: -10000000,
                                                                beta:   10000000,
                                                                maximizing));
                    }, i);
                }
                SetRunningTasks(tasks);
                pts      = maximizing ? int.MinValue : int.MaxValue;
                maxDepth = 999;
                for (int step = 0; step < threadCount; step++) {
                    minMaxRes = tasks[step].Result;
                    if (minMaxRes.BestMoveFound) {
                        permCount += minMaxRes.PermCount;
                        maxDepth   = Math.Min(maxDepth, minMaxRes.MaxDepth);
                        if (maximizing) {
                            if (minMaxRes.Pts > pts) {
                                pts      = minMaxRes.Pts;
                                bestMove = minMaxRes.BestMove;
                                retVal   = true;
                            }
                        } else {
                            if (minMaxRes.Pts < pts) {
                                pts      = minMaxRes.Pts;
                                bestMove = minMaxRes.BestMove;
                                retVal   = true;
                            }
                        }
                    }
                }
                if (maxDepth == 999) {
                    maxDepth = -1;
                }
                SetRunningTasks(tasks: null);
            } else {
                TBoard      chessBoardTmp;

                chessBoardTmp = (TBoard)board.Clone();
                minMaxRes     = FindBestMoveUsingAlphaBetaAsync(chessBoardTmp,
                                                                searchEngineSetting,
                                                                transTable,
                                                                playerId,
                                                                moveList,
                                                                player1PosInfo,
                                                                player2PosInfo,
                                                                moveList.Count,
                                                                alpha: -10000000,
                                                                beta:   10000000,
                                                                maximizing);
                permCount = minMaxRes.PermCount;
                maxDepth  = minMaxRes.MaxDepth;
                if (minMaxRes.BestMoveFound) {
                    bestMove = minMaxRes.BestMove;
                    retVal = true;
                }
            }
            cacheHit = transTable?.CacheHit ?? 0;
            return retVal;
        }
    } // Class SearchEngineAlphaBeta
} // Namespace
