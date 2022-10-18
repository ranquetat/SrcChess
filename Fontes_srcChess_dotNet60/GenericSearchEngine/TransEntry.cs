namespace GenericSearchEngine {

    /// <summary>
    /// Defines the base information kept in the translation table
    /// </summary>
    /// <remarks>
    internal struct TransEntry {

        /// <summary>64 bits key compute with Zobrist algorithm. Defined a probably unique board position</summary>
        public long Key64;
        /// <summary>Generation of the entry</summary>
        public long Generation;
        /// <summary>Value of the entry</summary>
        public int  Value;
        /// <summary>Extra info if any</summary>
        public int  ExtraInfo;
        /// <summary>Entry Weight. Higher means better</summary>
        public int  Weight;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="key64">      64 bits key compute with Zobrist algorithm. Defined a probably unique board position.</param>
        /// <param name="extraInfo">  Extra info</param>
        /// <param name="generation"> Generation of the entry</param>
        /// <param name="value">      Entry value</param>
        /// <param name="weight">     Entry weight</param>
        internal TransEntry(long key64,
                            int  extraInfo,
                            long generation,
                            int  value,
                            int  weight) {
            Key64      = key64;
            ExtraInfo  = extraInfo;
            Generation = generation;
            Value      = value;
            Weight     = weight;
        }
    }
}
