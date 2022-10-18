using System;
using System.Globalization;
using System.Text;

namespace GenericSearchEngine {

    /// <summary>Search engine setting</summary>
    public class SearchEngineSetting : ICloneable {

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="searchOption">         Search options</param>
        /// <param name="threadingMode">        Threading mode</param>
        /// <param name="searchDepth">          Search depth</param>
        /// <param name="timeOutInSec">         Timeout in second</param>
        /// <param name="randomMode">           Random mode</param>
        /// <param name="transTableEntryCount"> Size of the translation table</param>
        public SearchEngineSetting(SearchOption  searchOption,
                                   ThreadingMode threadingMode,
                                   int           searchDepth,
                                   int           timeOutInSec,
                                   RandomMode    randomMode,
                                   int           transTableEntryCount) {
            SearchOption         = searchOption;
            ThreadingMode        = threadingMode;
            SearchDepth          = searchDepth;
            TimeOutInSec         = timeOutInSec;
            RandomMode           = randomMode;
            TransTableEntryCount = transTableEntryCount;
        }

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="searchEngineSetting">  Search engine setting</param>
        protected SearchEngineSetting(SearchEngineSetting searchEngineSetting) : this(searchEngineSetting.SearchOption,
                                                                                      searchEngineSetting.ThreadingMode,
                                                                                      searchEngineSetting.SearchDepth,
                                                                                      searchEngineSetting.TimeOutInSec,
                                                                                      searchEngineSetting.RandomMode,
                                                                                      searchEngineSetting.TransTableEntryCount) { }

        /// <summary>
        /// Ctor
        /// </summary>
        public SearchEngineSetting() : this(SearchOption.UseAlphaBeta, ThreadingMode.OnePerProcessorForSearch, searchDepth: 2, timeOutInSec: 0, RandomMode.On, transTableEntryCount: 1000000) { }

        /// <summary>
        /// Clone the object
        /// </summary>
        /// <returns>
        /// New copy of the object
        /// </returns>
        public virtual object Clone() => new SearchEngineSetting(this);

        /// <summary>
        /// Verify if the two setting are the same
        /// </summary>
        /// <param name="searchEngineSetting">  Search engine setting to compare with</param>
        /// <returns>
        /// true if same setting, false if not
        /// </returns>
        protected virtual bool IsSameSettingInt(SearchEngineSetting searchEngineSetting)
            => searchEngineSetting.SearchOption         == SearchOption         &&
               searchEngineSetting.ThreadingMode        == ThreadingMode        &&
               searchEngineSetting.SearchDepth          == SearchDepth          &&
               searchEngineSetting.TimeOutInSec         == TimeOutInSec         &&
               searchEngineSetting.RandomMode           == RandomMode           &&
               searchEngineSetting.TransTableEntryCount == TransTableEntryCount;

        /// <summary>
        /// Verify if the two setting are the same
        /// </summary>
        /// <param name="searchEngineSetting">  Search engine setting to compare with</param>
        /// <returns>
        /// true if same setting, false if not
        /// </returns>
        public bool IsSameSetting(SearchEngineSetting searchEngineSetting) => IsSameSettingInt(searchEngineSetting);

        /// <summary>
        /// Search option
        /// </summary>
        public SearchOption SearchOption { get; set; }

        /// <summary>
        /// Threading option
        /// </summary>
        public ThreadingMode ThreadingMode { get; set; }

        /// <summary>
        /// Maximum search depth (or 0 to use iterative deepening depth-first search with time out)
        /// </summary>
        public int SearchDepth { get; set; }

        /// <summary>
        /// Time out in second if using iterative deepening depth-first search
        /// </summary>
        public int TimeOutInSec { get; set; }

        /// <summary>Random mode</summary>
        public RandomMode RandomMode { get; set; }

        /// <summary>
        /// Numbers of entry in the translation table if any
        /// </summary>
        public int TransTableEntryCount { get; set; }

        /// <summary>
        /// Gets human search mode
        /// </summary>
        /// <param name="bookMode"> Book mode if any</param>
        /// <returns>
        /// Search mode
        /// </returns>
        protected string HumanSearchMode(string bookMode) {
            StringBuilder strb = new();
            ThreadingMode threadingMode;
            int           processorCount;
            string        s;

            if ((SearchOption & SearchOption.UseAlphaBeta) == SearchOption.UseAlphaBeta) {
                strb.Append("Alpha-Beta. ");
            } else {
                strb.Append("Min-Max. ");
            }
            if (SearchDepth == 0) {
                strb.Append($"(Iterative {TimeOutInSec.ToString(CultureInfo.InvariantCulture)} secs). ");
            } else if ((SearchOption & SearchOption.UseIterativeDepthSearch) == SearchOption.UseIterativeDepthSearch) {
                strb.Append($"(Iterative {SearchDepth.ToString(CultureInfo.InvariantCulture)} ply). ");
            } else {
                strb.Append($"Fixed depth {SearchDepth.ToString(CultureInfo.InvariantCulture)} ply). ");
            }
            strb.Append($"{bookMode}");
            if ((SearchOption & SearchOption.UseTransTable) != 0) {
                strb.Append($"Translation table of {TransTableEntryCount} entries. ");
            }
            threadingMode  = ThreadingMode;
            processorCount = Environment.ProcessorCount;
            switch (threadingMode) {
            case ThreadingMode.Off:
                processorCount = 1;
                break;
            case ThreadingMode.DifferentThreadForSearch:
                processorCount = (processorCount >= 2) ? 2 : 1;
                break;
            default:
                break;
            }
            s = (processorCount == 1) ? "" : "s";
            strb.Append($"Using {processorCount.ToString(CultureInfo.InvariantCulture)} processor{s}.");
            return (strb.ToString());
        }

        /// <summary>
        /// Gets human search mode
        /// </summary>
        /// <returns>
        /// Search mode
        /// </returns>
        public virtual string HumanSearchMode() => HumanSearchMode("");

    }
}
