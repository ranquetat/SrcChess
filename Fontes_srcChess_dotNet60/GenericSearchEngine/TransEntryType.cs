namespace GenericSearchEngine {
    /// <summary>
    /// Type of transposition entry
    /// </summary>
    public enum TransEntryType {
        /// <summary>Exact move value</summary>
        Exact = 0,
        /// <summary>Alpha cut off value</summary>
        Alpha = 1,
        /// <summary>Beta cut off value</summary>
        Beta  = 2
    };
}
