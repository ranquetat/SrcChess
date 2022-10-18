
namespace GenericSearchEngine {
    
    /// <summary>
    /// Trace use by the search engine
    /// </summary>
    public interface ISearchTrace<TMove> where TMove : struct {

        /// <summary>
        /// Log a search trace
        /// </summary>
        /// <param name="depth">    Depth</param>
        /// <param name="playerId"> Player id</param>
        /// <param name="move">     Move</param>
        /// <param name="pts">      Points</param>
        void LogSearchTrace(int depth, int playerId, TMove move, int pts);
    }
}
