using System;

namespace GenericSearchEngine {

    /// <summary>
    /// Implements a transposition table. Transposition table is used to cache already computed board 
    /// </summary>
    public sealed class TransTable {

        /// <summary>Locking object</summary>
        private readonly object         m_lock = new();
        /// <summary>Hashlist of entries for player 1</summary>
        private readonly TransEntry[][] m_transEntries;
        /// <summary>Number of cache hit</summary>
        private int                     m_cacheHit = 0;
        /// <summary>Current generation</summary>
        private long                    m_curGen = 1;
        /// <summary>Entry count</summary>
        private readonly int            m_entryCount;
#if TransTableTrace
        /// <summary>Trace file</summary>
        private System.IO.StreamWriter  m_traceFile;
        /// <summary>Trace sequence</summary>
        private long                    m_traceSeq = 0;
#endif

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="entryCount"> Number of entries in the dictionary</param>
        /// <param name="perPlayer">  true if the translation table register info on a per player base or not</param>
        public TransTable(int entryCount, bool perPlayer) {
            if (entryCount > 2147483647) {
                throw new ArgumentException("Translation Table to big", nameof(entryCount));
            }
            m_transEntries = new TransEntry[2][];
            m_entryCount   = entryCount;
            if (perPlayer) {
                m_transEntries[0] = new TransEntry[m_entryCount];
                m_transEntries[1] = new TransEntry[m_entryCount];
            } else {
                m_transEntries[0] = new TransEntry[m_entryCount];
                m_transEntries[1] = m_transEntries[0];
            }
#if TransTableTrace
            string dir      = Environment.CurrentDirectory;
            string fileName = System.IO.Path.Combine(dir, "TransTrace.txt");
            m_traceFile     = System.IO.File.CreateText(fileName);
#endif
        }

#if TransTableTrace
        private void TraceLog(string str) {
            m_traceFile.WriteLine(str);
            m_traceFile.Flush();
        }
#endif

        /// <summary>
        /// Size of the translation table
        /// </summary>
        public int EntryCount => m_entryCount;

        /// <summary>
        /// Gets the entry position for the specified key
        /// </summary>
        /// <param name="zobristKey"> Zobrist key</param>
        /// <returns>
        /// Gets the entry position
        /// </returns>
        private int GetEntryPos(long zobristKey) => (int)((ulong)zobristKey % (uint)m_entryCount);

        /// <summary>
        /// Record a new entry in the table
        /// </summary>
        /// <param name="playerId">   Player id</param>
        /// <param name="zobristKey"> Zobrist key. Probably unique for this board position.</param>
        /// <param name="extraInfo">  Extra info</param>
        /// <param name="value">      Value</param>
        /// <param name="weight">     Weight</param>
        public void RecordEntry(int playerId, long zobristKey, int extraInfo, int value, int weight) {
            TransEntry entry;
            int        entryPos;

#if TransTableTrace
            long oriZobristKey = zobristKey;
#endif
            zobristKey ^= extraInfo;
            entryPos    = GetEntryPos(zobristKey);
            entry       = new(zobristKey, extraInfo, m_curGen, value, weight);
            lock (m_lock) {
                m_transEntries[playerId][entryPos] = entry;
            }
#if TransTableTrace
            TraceLog($"Trace Seq: {++m_traceSeq} Gen: {m_curGen} RecordEntry - ZobristKey: {zobristKey} ({oriZobristKey}) ExtraInfo: {extraInfo} Depth: {depth} Type: {entryType} Value: {value}");
#endif
        }

        /// <summary>
        /// Try to find if the current board has already been evaluated
        /// </summary>
        /// <param name="playerId">   Player id</param>
        /// <param name="zobristKey"> Zobrist key. Probably unique for this board position.</param>
        /// <param name="extraInfo">  Extra info</param>
        /// <param name="weight">     Entry weight</param>
        /// <returns>
        /// int.MaxValue if no valid value found, else value of the board.
        /// </returns>
        public int ProbeEntry(int playerId, long zobristKey, int extraInfo, int weight) {
            int        retVal = int.MaxValue;
            int        entryPos;
            TransEntry entry;

#if TransTableTrace
            long oriZobristKey = zobristKey;
#endif
            zobristKey ^= extraInfo;
            entryPos    = GetEntryPos(zobristKey);
            lock (m_lock) {
                entry = m_transEntries[playerId][entryPos];
                if (entry.Key64 == zobristKey && entry.Generation == m_curGen && entry.ExtraInfo == extraInfo && entry.Weight >= weight) {
                    retVal = entry.Value;
                    m_cacheHit++;
#if TransTableTrace
                    TraceLog($"Trace Seq: {++m_traceSeq} Gen: {m_curGen} ProbeEntry - {zobristKey} ({oriZobristKey}) Weight: {entry.Weight} Value: {retVal}");
#endif
                }
            }
            return retVal;
        }

        /// <summary>
        /// Number of cache hit
        /// </summary>
        public int CacheHit => m_cacheHit;

        /// <summary>
        /// Reset the cache
        /// </summary>
        public void Reset() {
            m_cacheHit = 0;
            m_curGen++;
#if TransTableTrace
            TraceLog($"Trace Seq: {++m_traceSeq} Gen: {m_curGen} Reset");
#endif
        }

        /// <summary>
        /// Reset the cache
        /// </summary>
        public void ResetCacheHit() => m_cacheHit = 0;

    } // Class TransTable
}
