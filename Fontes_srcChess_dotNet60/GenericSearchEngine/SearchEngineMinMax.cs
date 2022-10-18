using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GenericSearchEngine {

    /// <summary>
    /// MinMax Search Engine
    /// </summary>
    internal sealed class SearchEngineMinMax<TBoard, TMove> : SearchEngine<TBoard, TMove> where TMove : struct
                                                                                          where TBoard : IGameBoard<TMove> {

        /// <summary>
        /// Class constructor
        /// </summary>
        /// <param name="trace">  Trace object or null</param>
        /// <param name="rnd">    Random object</param>
        /// <param name="rndRep"> Repetitive random object</param>
        public SearchEngineMinMax(ISearchTrace<TMove>? trace, Random rnd, Random rndRep) : base(trace, rnd, rndRep) { }

        /// <summary>
        /// Recursive Min/Max routine using MinMaxInfo structure
        /// </summary>
        /// <param name="moveListPlayerId"> Player doing the move</param>
        /// <param name="board">            Chess board</param>
        /// <param name="moveList">         Move list</param>
        /// <param name="minMaxInfo">       MinMax info</param>
        /// <param name="bestMovePos">      Best move position</param>
        /// <param name="isFullyEvaluated"> true if all moves has been evaluated</param>
        /// <param name="maximizing">       true if maximizing, false if minimizing</param>
        /// <returns>
        /// Points to give for this move
        /// </returns>
        private static int MinMax(int         moveListPlayerId,
                                  TBoard      board,
                                  List<TMove> moveList,
                                  MinMaxInfo  minMaxInfo,
                                  out int     bestMovePos,
                                  out bool    isFullyEvaluated,
                                  bool        maximizing) {
            int         value;
            int         lastMovePlayerId;
            int         moveIndex;
            List<TMove> childMoveList;
            int         boardExtraInfo;

            bestMovePos      = -1;
            lastMovePlayerId = 1 - moveListPlayerId;
            isFullyEvaluated = true;
            if (!board.IsMoveTerminal(moveListPlayerId,
                                      moveList,
                                      minMaxInfo,
                                      IsSearchHasBeenCanceled,
                                      out int retVal)) {
                moveIndex = 0;
                retVal    = maximizing ? int.MinValue : int.MaxValue;
                foreach (TMove move in moveList) {
                    if (board.DoMoveNoLog(move)) {
                        boardExtraInfo = board.ComputeBoardExtraInfo();
                        value          = minMaxInfo.TransTable?.ProbeEntry(moveListPlayerId, board.ZobristKey, boardExtraInfo, minMaxInfo.Depth - 1) ?? int.MaxValue;
                        if (value == int.MaxValue) {
                            childMoveList = board.GetMoves(lastMovePlayerId, out AttackPosInfo _);
                            minMaxInfo.Depth--;
                            value = MinMax(lastMovePlayerId,
                                           board,
                                           childMoveList,
                                           minMaxInfo,
                                           out int _,
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
                        value = 0;
                        board.UndoMoveNoLog(move);
                    }
                    if (maximizing) {
                        if (value > retVal) {
                            retVal      = value;
                            bestMovePos = moveIndex;
                        }
                    } else if (value < retVal) {
                        retVal      = value;
                        bestMovePos = moveIndex;
                    }
                    moveIndex++;
                }
            }
            return retVal;
        }

        /// <summary>
        /// Find the best move for a player using alpha-beta for a given depth
        /// </summary>
        /// <param name="board">               Chess board</param>
        /// <param name="searchEngineSetting"> Search mode</param>
        /// <param name="transTable">          Translation table if any</param>
        /// <param name="playerId">            Color doing the move</param>
        /// <param name="moveList">            Move list</param>
        /// <param name="player1PosInfo">      Attack position information for player 1</param>
        /// <param name="player2PosInfo">      Attack position information for player 2</param>
        /// <param name="maxDepth">            Maximum depth</param>
        /// <param name="totalMoveCount">      Total move count</param>
        /// <param name="maximizing">          true for maximizing, false for minimizing</param>
        /// <param name="bestMove">            Best move found</param>
        /// <param name="pts">                 Best move points</param>
        /// <param name="permCount">           Total permutation evaluated</param>
        /// <returns>
        /// true if a move has been found
        /// </returns>
        private bool FindBestMoveUsingMinMaxAtDepth(TBoard              board,
                                                    SearchEngineSetting searchEngineSetting,
                                                    TransTable?         transTable,
                                                    int                 playerId,
                                                    List<TMove>         moveList,
                                                    AttackPosInfo       player1PosInfo,
                                                    AttackPosInfo       player2PosInfo,
                                                    int                 maxDepth,
                                                    int                 totalMoveCount,
                                                    bool                maximizing,
                                                    out TMove           bestMove,
                                                    out int             pts,
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
            minMaxInfo = new(searchEngineSetting, transTable) {
                PermCount            = 0,
                Depth                = maxDepth,
                MaxDepth             = maxDepth,
                TimeOut              = DateTime.MaxValue,
                Player1AttackPosInfo = player1PosInfo,
                Player2AttackPosInfo = player2PosInfo,
                Player1MoveCount     = player1MoveCount,
                Player2MoveCount     = player2MoveCount
            };            
            pts = MinMax(playerId,
                         board,
                         moveList,
                         minMaxInfo,
                         out int bestMovePos,
                         isFullyEvaluated: out bool _,
                         maximizing);
            if (bestMovePos != -1) {
                bestMove = moveList[bestMovePos];
                retVal   = true;
                LogSearchTrace(maxDepth, playerId, bestMove, pts);
            } else {
                bestMove = board.CreateEmptyMove();
            }
            permCount = minMaxInfo.PermCount;
            return retVal;
        }

        /// <summary>
        /// Find the best move for a player using minmax search.
        /// Handle the search method: Fix depth or time limited
        /// One or more thread can execute this method at the same time.
        /// </summary>
        /// <param name="board">               Board</param>
        /// <param name="searchEngineSetting"> Search engine setting</param>
        /// <param name="transTable">          Translation table</param>
        /// <param name="playerId">            Player id</param>
        /// <param name="moveList">            List of moves to evaluate</param>
        /// <param name="player1PosInfo">      Attack position information for player 1</param>
        /// <param name="player2PosInfo">      Attack position information for player 2</param>
        /// <param name="totalMoveCount">      Total move count</param>
        /// <param name="maximizing">          true for maximizing, false for minimizing</param>
        /// <returns>
        /// MinMax result
        /// </returns>
        private MinMaxResult<TMove> FindBestMoveUsingMinMaxAsync(TBoard              board,
                                                                 SearchEngineSetting searchEngineSetting,
                                                                 TransTable?         transTable,
                                                                 int                 playerId,
                                                                 List<TMove>         moveList,
                                                                 AttackPosInfo       player1PosInfo,
                                                                 AttackPosInfo       player2PosInfo,
                                                                 int                 totalMoveCount,
                                                                 bool                maximizing) {
            MinMaxResult<TMove> retVal;
            DateTime            timeOut;
            ThreadPriority      threadPriority;
            int                 depth;

            threadPriority                = Thread.CurrentThread.Priority;
            Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
            retVal                        = new MinMaxResult<TMove>();
            try {
                if (searchEngineSetting.SearchDepth == 0) {
                    timeOut = DateTime.Now + TimeSpan.FromSeconds(searchEngineSetting.TimeOutInSec);
                    depth = 0;
                    do {
                        retVal.BestMoveFound = FindBestMoveUsingMinMaxAtDepth(board,
                                                                              searchEngineSetting,
                                                                              transTable,
                                                                              playerId,
                                                                              moveList,
                                                                              player1PosInfo,
                                                                              player2PosInfo,
                                                                              depth + 1,
                                                                              totalMoveCount,
                                                                              maximizing,
                                                                              out TMove bestMove,
                                                                              out int pts,
                                                                              out int permCountAtLevel);
                        retVal.Pts        = pts;
                        retVal.PermCount += permCountAtLevel;
                        retVal.BestMove   = bestMove;
                        depth++;
                    } while (DateTime.Now < timeOut);
                    retVal.MaxDepth = depth;
                } else {
                    retVal.MaxDepth      = searchEngineSetting.SearchDepth;
                    retVal.BestMoveFound = FindBestMoveUsingMinMaxAtDepth(board,
                                                                          searchEngineSetting,
                                                                          transTable,
                                                                          playerId,
                                                                          moveList,
                                                                          player1PosInfo,
                                                                          player2PosInfo,
                                                                          retVal.MaxDepth,
                                                                          totalMoveCount,
                                                                          maximizing,
                                                                          out TMove bestMove,
                                                                          out int pts,
                                                                          out int permCount);
                    retVal.Pts       = pts;
                    retVal.PermCount = permCount;
                    retVal.BestMove  = bestMove;
                }
            } finally {
                Thread.CurrentThread.Priority = threadPriority;
            }
            return retVal;
        }

        /// <summary>
        /// Find the best move for a player using MinMax search.
        /// Handle the number of thread assignment for the search.
        /// </summary>
        /// <param name="board">               Chess board</param>
        /// <param name="searchEngineSetting"> Search mode</param>
        /// <param name="playerId">            Color doing the move</param>
        /// <param name="moveList">            Move list</param>
        /// <param name="attackPosInfo">       Information about pieces attacks</param>
        /// <param name="bestMove">            Best move found</param>
        /// <param name="permCount">           Nb of permutations evaluated</param>
        /// <param name="cacheHit">            Nb of cache hit</param>
        /// <param name="maxDepth">            Maximum depth evaluated</param>
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
            int                         pts;
            TransTable?                 transTable;
            int                         threadCount;
            int                         moveListPos;
            int                         moveListCount;
            int                         movePerTask;
            TBoard[]                    boards;
            List<TMove>[]               taskMoveList;
            Task<MinMaxResult<TMove>>[] tasks;
            AttackPosInfo               player1PosInfo;
            AttackPosInfo               player2PosInfo;
            MinMaxResult<TMove>         minMaxRes;
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
                boards       = new TBoard[threadCount];
                taskMoveList = new List<TMove>[threadCount];
                tasks        = new Task<MinMaxResult<TMove>>[threadCount];
                moveListPos  = 0;
                movePerTask  = moveListCount / threadCount + ((moveListCount % threadCount) != 0 ? 1 : 0);
                for (int i = 0; i < threadCount; i++) {
                    boards[i]       = (TBoard)board.Clone();
                    taskMoveList[i] = new List<TMove>(movePerTask);
                    int j = 0;
                    while (j < movePerTask && moveListPos < moveListCount) {
                        taskMoveList[i].Add(moveList[moveListPos++]);
                        j++;
                    }
                }
                for (int i = 0; i < threadCount; i++) {
                    tasks[i] = Task<MinMaxResult<TMove>>.Factory.StartNew((param) => {
                        int step = (int)param!;
                        return FindBestMoveUsingMinMaxAsync(boards[step],
                                                            searchEngineSetting,
                                                            transTable,
                                                            playerId,
                                                            taskMoveList[step],
                                                            player1PosInfo,
                                                            player2PosInfo,
                                                            moveList.Count,
                                                            maximizing);
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
                        if (maximizing  && minMaxRes.Pts > pts ||
                            !maximizing && minMaxRes.Pts < pts) {
                            pts      = minMaxRes.Pts;
                            bestMove = minMaxRes.BestMove;
                            retVal   = true;
                        }
                    }
                }
                if (maxDepth == 999) {
                    maxDepth = -1;
                }
                SetRunningTasks(tasks: null);
            } else {
                TBoard tmpBoard;

                tmpBoard  = (TBoard)board.Clone();
                minMaxRes = FindBestMoveUsingMinMaxAsync(tmpBoard,
                                                         searchEngineSetting,
                                                         transTable,
                                                         playerId,
                                                         moveList,
                                                         player1PosInfo,
                                                         player2PosInfo,
                                                         moveList.Count,
                                                         maximizing);
                if (minMaxRes.BestMoveFound) {
                    permCount = minMaxRes.PermCount;
                    maxDepth  = minMaxRes.MaxDepth;
                    bestMove  = minMaxRes.BestMove;
                    retVal    = true;
                } else { 
                    maxDepth = -1;
                }
            }
            cacheHit = transTable?.CacheHit ?? 0;
            return retVal;
        }
    } // Class SearchEngineMinMax
} // Namespace
