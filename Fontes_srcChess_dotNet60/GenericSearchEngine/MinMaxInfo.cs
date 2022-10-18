using System;

namespace GenericSearchEngine {

    /// <summary>
    /// Defines the MinMax and Alpha Beta info
    /// </summary>
    public class MinMaxInfo {
        /// <summary>Search mode</summary>
        public SearchEngineSetting SearchEngineSetting { get; private set; }
        /// <summary>Translation table if any</summary>
        public TransTable?         TransTable { get; private set; }
        /// <summary>Time before timeout. Use for iterative</summary>
        public DateTime            TimeOut { get; set; }
        /// <summary>true if the search has timed out</summary>
        public bool                HasTimedOut { get; set; }
        /// <summary>Number of board evaluated</summary>
        public int                 PermCount { get; set; }
        /// <summary>Current search depth</summary>
        public int                 Depth { get; set; }
        /// <summary>Maximum depth to search</summary>
        public int                 MaxDepth { get; set; }
        /// <summary>Information about pieces attacks</summary>
        public AttackPosInfo       Player1AttackPosInfo { get; set; }
        /// <summary>Information about pieces attacks</summary>
        public AttackPosInfo       Player2AttackPosInfo { get; set; }
        /// <summary>Number of move available for player 1</summary>
        public int                 Player1MoveCount { get; set; }
        /// <summary>Number of move available for player 2</summary>
        public int                 Player2MoveCount { get; set; }

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="searchEngineSetting"> Search engine setting</param>
        /// <param name="transTable">          Translation table if any</param>
        /// 
        public MinMaxInfo(SearchEngineSetting searchEngineSetting, TransTable? transTable) {
            SearchEngineSetting = searchEngineSetting;
            TransTable          = transTable;
        }
    }
}
