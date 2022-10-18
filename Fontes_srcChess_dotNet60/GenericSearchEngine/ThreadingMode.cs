namespace GenericSearchEngine {

    /// <summary>Threading mode</summary>
    public enum ThreadingMode {
        /// <summary>No threading at all. User interface share the search one.</summary>
        Off                      = 0,
        /// <summary>Use a background thread for search</summary>
        DifferentThreadForSearch = 1,
        /// <summary>Use one background thread for each processor for search</summary>
        OnePerProcessorForSearch = 2
    }
}
