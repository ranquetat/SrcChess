using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GenericSearchEngine {

    public abstract class SearchEngine {
        /// <summary>Id of the first player</summary>
        public const int PlayerId1 = 0;
        /// <summary>Id of the second player</summary>
        public const int PlayerId2 = 1;
    }

    /// <summary>
    /// Search engine base class
    /// </summary>
    public abstract class SearchEngine<TBoard, TMove> : SearchEngine where TMove  : struct
                                                                     where TBoard : IGameBoard<TMove> {

        /// <summary>
        /// Index Point structure
        /// </summary>
        private struct PointIndex : IComparable<PointIndex> {
            public int Index;
            public int Points;

            public int CompareTo(PointIndex Other) {
                int retVal;

                if (Points < Other.Points) {
                    retVal = 1;
                } else if (Points > Other.Points) {
                    retVal = -1;
                } else {
                    retVal = (Index < Other.Index) ? -1 : 1;
                }
                return (retVal);
            }
        } // Class IndexPoint

        #region Members
        /// <summary>Working search engine</summary>
        private static   SearchEngine<TBoard, TMove>? m_workingSearchEngine = null;
        /// <summary>Object where to redirect the trace if any</summary>
        private readonly ISearchTrace<TMove>?         m_trace;
        /// <summary>Random number generator</summary>
        private readonly Random                       m_rnd;
        /// <summary>Random number generator (repetitive, seed = 0)</summary>
        private readonly Random                       m_rndRep;
        /// <summary>Synchronize the tasks access</summary>
        private static readonly object                m_lock = new();
        /// <summary>Running tasks</summary>
        private static Task<MinMaxResult<TMove>>[]?   m_tasks = null;
        #endregion

        /// <summary>
        /// Class constructor
        /// </summary>
        /// <param name="trace">  Trace object or null</param>
        /// <param name="rnd">    Random object</param>
        /// <param name="rndRep"> Repetitive random object</param>
        protected SearchEngine(ISearchTrace<TMove>? trace, Random rnd, Random rndRep) {
            m_trace  = trace;
            m_rnd    = rnd;
            m_rndRep = rndRep;
        }

        /// <summary>true to cancel the search</summary>
        protected static bool SearchCancelState { get; set; } = false;

        /// <summary>
        /// Debugging routine
        /// </summary>
        /// <param name="depth">       Actual search depth</param>
        /// <param name="playerColor"> Color doing the move</param>
        /// <param name="move">        Move</param>
        /// <param name="pts">         Points for this move</param>
        protected void LogSearchTrace(int depth, int playerId, TMove move, int pts) => m_trace?.LogSearchTrace(depth, playerId, move, pts);

        /// <summary>
        /// Cancel the search
        /// </summary>
        public static void CancelSearch() => SearchCancelState = true;

        /// <summary>
        /// Return true if search engine is busy
        /// </summary>
        public static bool IsSearchEngineBusy => m_workingSearchEngine != null;

        /// <summary>
        /// Return true if the search has been canceled
        /// </summary>
        public static bool IsSearchHasBeenCanceled => SearchCancelState;

        /// <summary>
        /// Sort move list using the specified point array so the highest point move come first
        /// </summary>
        /// <param name="moveList">     Source move list to sort</param>
        /// <param name="points">       Array of points for each move</param>
        /// <param name="pointsCount">  Number of points we must consider</param>
        /// <returns>
        /// Sorted move list
        /// </returns>
        protected static List<TMove> SortMoveList(List<TMove> moveList, int[] points, int pointsCount) {
            List<TMove>  retVal;
            PointIndex[] pointIndexes;

            retVal       = new List<TMove>(moveList.Count);
            pointIndexes = new PointIndex[points.Length];
            for (int i = 0; i < pointsCount; i++) {
                pointIndexes[i].Points = points[i];
                pointIndexes[i].Index  = i;
            }
            Array.Reverse(pointIndexes);
            Array.Sort<PointIndex>(pointIndexes);
            for (int i = 0; i < pointsCount; i++) {
                retVal.Add(moveList[pointIndexes[i].Index]);
            }
            for (int i = pointsCount; i < moveList.Count; i++) {
                retVal.Add(moveList[i]);
            }
            return (retVal);
        }

        /// <summary>
        /// Find the best move using a specific search method
        /// </summary>
        /// <param name="board">               Board</param>
        /// <param name="searchEngineSetting"> Search engine setting</param>
        /// <param name="playerId">            Player doing the move</param>
        /// <param name="moveList">            Move list</param>
        /// <param name="attackPosInfo">       Attack/defense position info</param>
        /// <param name="bestMove">            Best move found</param>
        /// <param name="permCount">           Total permutation evaluated</param>
        /// <param name="cacheHit">            Number of moves found in the translation table cache</param>
        /// <param name="maxDepth">            Maximum depth to use</param>
        /// <returns>
        /// true if a move has been found
        /// </returns>
        protected abstract bool FindBestMove(TBoard              board,
                                             SearchEngineSetting searchEngineSetting,
                                             int                 playerId,
                                             List<TMove>         moveList,
                                             AttackPosInfo       attackPosInfo,
                                             ref TMove           bestMove,
                                             out int             permCount,
                                             out long            cacheHit,
                                             out int             maxDepth);

        /// <summary>
        /// Shuffle move position if random mode is on
        /// </summary>
        /// <param name="moveList"> Move list to randomize</param>
        /// <param name="rnd">      Random number generator</param>
        private static void ShuffleMovePos(List<TMove> moveList, Random rnd) {
            int   swapIndex;
            TMove tmp;

            for (int i = 0; i < moveList.Count; i++) {
                swapIndex           = rnd.Next(moveList.Count);
                tmp                 = moveList[i];
                moveList[i]         = moveList[swapIndex];
                moveList[swapIndex] = tmp;
            }
        }

        /// <summary>
        /// Find the best move for a player using a specific method
        /// </summary>
        /// <param name="board">               Board</param>
        /// <param name="searchEngineSetting"> Search engine setting</param>
        /// <param name="playerId">            Player making the move</param>
        /// <param name="useDispatcher">       True to use a dispatcher to call the moving action function in the UI thread (when function is called on a background thread</param>
        /// <param name="cookie">              Cookie to pass to the action</param>
        /// <param name="foundMoveAction">     Action to execute when a move has been found</param>
        private void FindBestMove<T>(TBoard              board,
                                     SearchEngineSetting searchEngineSetting,
                                     int                 playerId,
                                     bool                useDispatcher,
                                     T                   cookie,
                                     Action<T,object?>?  foundMoveAction) {
            List<TMove> moveList;
            TMove       move;
            Random      rnd;

            moveList = board.GetMoves(playerId, out AttackPosInfo attackPosInfo);
            if (searchEngineSetting.RandomMode != RandomMode.Off) {
                rnd = (searchEngineSetting.RandomMode == RandomMode.OnRepetitive) ? m_rndRep : m_rnd;
                ShuffleMovePos(moveList, rnd);
            }
            move = board.CreateEmptyMove();
            FindBestMove(board, searchEngineSetting, playerId, moveList, attackPosInfo, ref move, out int permCount, out long cacheHit, out int maxDepth);
            m_workingSearchEngine = null;
            board.InvokeMoveAction<T>(move, cookie, permCount, cacheHit, maxDepth, useDispatcher, foundMoveAction);
        }

        /// <summary>
        /// Find the best move for the given player
        /// </summary>
        /// <param name="trace">               Trace object or null</param>
        /// <param name="rnd">                 Random object</param>
        /// <param name="rndRep">              Repetitive random object</param>
        /// <param name="board">               Board</param>
        /// <param name="searchEngineSetting"> Search engine setting</param>
        /// <param name="playerId">            Player making the move</param>
        /// <param name="foundMoveAction">     Action to execute when the find best move routine is done</param>
        /// <param name="cookie">              Cookie to pass to the actionMoveFound action</param>
        /// <returns>
        /// true if search has started, false if search engine is busy
        /// </returns>
        public static bool FindBestMove<T>(ISearchTrace<TMove>? trace,
                                           Random               rnd,
                                           Random               rndRep,
                                           TBoard               board,
                                           SearchEngineSetting  searchEngineSetting,
                                           int                  playerId,
                                           Action<T,object?>?   foundMoveAction,
                                           T                    cookie) {
            bool                       retVal;
            bool                       isMultipleThread;
            SearchEngine<TBoard,TMove> searchEngine;

            retVal = !IsSearchEngineBusy;
            if (retVal) {
                SearchCancelState = false;
                if ((searchEngineSetting.SearchOption & SearchOption.UseAlphaBeta) == SearchOption.UseMinMax) {
                    searchEngine = new SearchEngineMinMax<TBoard,TMove>(trace, rnd, rndRep);
                } else {
                    searchEngine = new SearchEngineAlphaBeta<TBoard,TMove>(trace, rnd, rndRep);
                }
                isMultipleThread      = (searchEngineSetting.ThreadingMode == ThreadingMode.DifferentThreadForSearch ||
                                         searchEngineSetting.ThreadingMode == ThreadingMode.OnePerProcessorForSearch);
                m_workingSearchEngine = searchEngine;
                if (isMultipleThread) {
                    Task.Factory.StartNew(() => searchEngine.FindBestMove<T>(board,
                                                                             searchEngineSetting,
                                                                             playerId,
                                                                             useDispatcher: true,
                                                                             cookie,
                                                                             foundMoveAction));
                } else {
                    searchEngine.FindBestMove(board,
                                              searchEngineSetting,
                                              playerId,
                                              useDispatcher: false,
                                              cookie,
                                              foundMoveAction);
                }
            }
            return (retVal);
        }

        /// <summary>
        /// Set the running tasks
        /// </summary>
        /// <param name="tasks"> Running tasks</param>
        internal static void SetRunningTasks(Task<MinMaxResult<TMove>>[]? tasks) {
            lock(m_lock) {
                m_tasks = tasks;
            }
        }

        /// <summary>
        /// Gets the number of running tasks or 0 if none
        /// </summary>
        /// <returns>
        /// Number of running tasks
        /// </returns>
        public static int GetRunningThreadCount() {
            int                          retVal = 0;
            Task<MinMaxResult<TMove>>[]? tasks;

            lock (m_lock) {
                tasks = m_tasks;
            }
            if (tasks != null) {
                foreach (Task? task in tasks) {
                    if (!task.IsCompleted) {
                        retVal++;
                    }
                }
            }
            return (retVal);
        }
    } // Class SearchEngine
} // Namespace
