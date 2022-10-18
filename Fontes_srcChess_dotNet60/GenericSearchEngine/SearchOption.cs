using System;

namespace GenericSearchEngine {

    /// <summary>Search options</summary>
    [Flags]
    public enum SearchOption {
        /// <summary>Use MinMax search</summary>
        UseMinMax               = 0,
        /// <summary>Use Alpha-Beta prunning function</summary>
        UseAlphaBeta            = 1,
        /// <summary>Use transposition table</summary>
        UseTransTable           = 2,
        /// <summary>Use iterative depth-first search on a fix ply count</summary>
        UseIterativeDepthSearch = 8
    }
}
