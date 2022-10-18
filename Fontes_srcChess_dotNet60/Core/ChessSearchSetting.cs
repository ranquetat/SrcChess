using System;
using System.Xml;
using GenericSearchEngine;

namespace SrcChess2.Core {

    /// <summary>
    /// Chess board setting can be set to preset difficulty level or to manual
    /// Manual setting is preserved even if we switch from or to preset difficulty level.
    /// To access the difficulty level setting, use the GetBoardSearchSetting(difficultyLevel) function which returns
    ///     - The specified preset difficulty level setting
    ///     - The actual manual difficulty level setting
    /// If the difficultyLevel is manual, the instance of the called class is returned. If not, a new instance
    /// of this class with the information for the preset difficultyLevel is returned.
    /// </summary>
    public class ChessSearchSetting : SearchEngineSetting {

        /// <summary>
        /// Opening book used by the computer
        /// </summary>
        public enum BookModeSetting {
            /// <summary>No opening book</summary>
            NoBook    = 0,
            /// <summary>Use a book built from unrated games</summary>
            Unrated   = 1,
            /// <summary>Use a book built from games by player with ELO greater then 2500</summary>
            ELOGT2500 = 2
        }

        /// <summary>
        /// Difficulty level
        /// </summary>
        public enum SettingDifficultyLevel {
            /// <summary>Manual</summary>
            Manual       = 0,
            /// <summary>Very easy: 2 ply, (no book, weak board evaluation for computer)</summary>
            VeryEasy     = 1,
            /// <summary>Easy: 2 ply, (no book, normal board evaluation for computer)</summary>
            Easy         = 2,
            /// <summary>Intermediate: 4 ply, (unrated book, normal board evaluation for computer)</summary>
            Intermediate = 3,
            /// <summary>Hard: 4 ply, (ELO 2500 book, normal board evaluation for computer)</summary>
            Hard         = 4,
            /// <summary>Hard: 6 ply, (ELO 2500 book, normal board evaluation for computer)</summary>
            VeryHard     = 5
        }

        /// <summary>Opening book create using EOL greater than 2500</summary>
        private static readonly Book? m_book2500;
        /// <summary>Opening book create using unrated games</summary>
        private static readonly Book? m_bookUnrated;


        /// <summary>
        /// Try to read a book from a file or resource if file is not found
        /// </summary>
        /// <param name="bookName"> Book name</param>
        /// <returns>
        /// Book
        /// </returns>
        private static Book? ReadBook(string bookName) {
            Book? retVal;
            bool  succeed = false;

            retVal = new Book();
            try {
                if (retVal.ReadBookFromFile(bookName + ".bin")) {
                    succeed = true;
                }
            } catch (Exception) {
            }
            if (!succeed) {
                try {
                    if (!retVal.ReadBookFromResource("SrcChess2." + bookName + ".bin")) {
                        retVal = null;
                    }
                } catch (Exception) {
                    retVal = null;
                }
            }
            return (retVal);
        }

        /// <summary>
        /// Static Ctor
        /// </summary>
        static ChessSearchSetting() {
            m_book2500    = ReadBook("Book2500");
            m_bookUnrated = ReadBook("BookUnrated");
        }

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="searchOption">         Search options</param>
        /// <param name="threadingMode">        Threading mode</param>
        /// <param name="searchDepth">          Search depth</param>
        /// <param name="timeOutInSec">         Timeout in second</param>
        /// <param name="randomMode">           Random mode</param>
        /// <param name="transTableEntryCount"> Size of the translation table</param>
        /// <param name="whiteBoardEval">       Board evaluation for white player</param>
        /// <param name="blackBoardEval">       Board evaluation for black player</param>
        /// <param name="bookMode">             Type of book to be used</param>
        /// <param name="difficultyLevel">      Difficulty level</param>
        public ChessSearchSetting(SearchOption           searchOption,
                                  ThreadingMode          threadingMode,
                                  int                    searchDepth,
                                  int                    timeOutInSec,
                                  RandomMode             randomMode,
                                  int                    transTableEntryCount,
                                  IBoardEvaluation?      whiteBoardEval,
                                  IBoardEvaluation?      blackBoardEval,
                                  BookModeSetting        bookMode,
                                  SettingDifficultyLevel difficultyLevel) : base(searchOption, threadingMode, searchDepth, timeOutInSec, randomMode, transTableEntryCount) {
            WhiteBoardEvaluator = whiteBoardEval ?? new BoardEvaluationBasic();
            BlackBoardEvaluator = blackBoardEval ?? new BoardEvaluationBasic();
            BookMode            = bookMode;
            DifficultyLevel     = difficultyLevel;
        }

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="whiteBoardEval">  Board evaluation for white player</param>
        /// <param name="blackBoardEval">  Board evaluation for black player</param>
        /// <param name="bookMode">        Type of book to be used</param>
        /// <param name="difficultyLevel"> Difficulty level</param>
        public ChessSearchSetting(SearchEngineSetting    searchEngineSetting,
                                  IBoardEvaluation?      whiteBoardEval,
                                  IBoardEvaluation?      blackBoardEval,
                                  BookModeSetting        bookMode,
                                  SettingDifficultyLevel difficultyLevel) : base(searchEngineSetting) {
            WhiteBoardEvaluator = whiteBoardEval ?? new BoardEvaluationBasic();
            BlackBoardEvaluator = blackBoardEval ?? new BoardEvaluationBasic();
            BookMode            = bookMode;
            DifficultyLevel     = difficultyLevel;
        }

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="chessSearchSetting"> Chess search setting</param>
        private ChessSearchSetting(ChessSearchSetting chessSearchSetting) : base(chessSearchSetting) {
            WhiteBoardEvaluator = chessSearchSetting.WhiteBoardEvaluator;
            BlackBoardEvaluator = chessSearchSetting.BlackBoardEvaluator;
            BookMode            = chessSearchSetting.BookMode;
            DifficultyLevel     = chessSearchSetting.DifficultyLevel;;
        }

        /// <summary>
        /// Ctor
        /// </summary>
        public ChessSearchSetting() : this(new SearchEngineSetting(), new BoardEvaluationBasic(), new BoardEvaluationBasic(), BookModeSetting.ELOGT2500, SettingDifficultyLevel.Easy) {}

        /// <summary>
        /// Clone the current object
        /// </summary>
        /// <returns>
        /// New clone
        /// </returns>
        public override object Clone() => new ChessSearchSetting(this);

        /// <summary>
        /// Verify if the two setting are the same
        /// </summary>
        /// <param name="searchEngineSetting"> Search engine setting to compare with</param>
        /// <returns>
        /// true if same setting, false if not
        /// </returns>
        protected override bool IsSameSettingInt(SearchEngineSetting searchEngineSetting) {
            bool retVal;

            if (searchEngineSetting is ChessSearchSetting chessSearchSetting) {
                retVal = base.IsSameSettingInt(chessSearchSetting)                               &&
                         WhiteBoardEvaluator           == chessSearchSetting.WhiteBoardEvaluator &&
                         BlackBoardEvaluator           == chessSearchSetting.BlackBoardEvaluator &&
                         WhiteBoardEvaluator.GetType() == BlackBoardEvaluator.GetType()          &&
                         BookMode                      == chessSearchSetting.BookMode            &&
                         DifficultyLevel               == chessSearchSetting.DifficultyLevel;
            } else {
                retVal = false;
            }
            return retVal;
        }

        /// <summary>
        /// White player board evaluator
        /// </summary>
        public IBoardEvaluation WhiteBoardEvaluator { get; set; }

        /// <summary>
        /// Black player board evaluator
        /// </summary>
        public IBoardEvaluation BlackBoardEvaluator { get; set; }

        /// <summary>
        /// Book builds from games of player having ELO greater or equal to 2500
        /// </summary>
        public static Book? Book2500 => m_book2500;

        /// <summary>
        /// Book builds from games of unrated player
        /// </summary>
        public static Book? BookUnrated => m_bookUnrated;

        /// <summary>
        /// Difficulty level
        /// </summary>
        public SettingDifficultyLevel DifficultyLevel { get; set; }

        /// <summary>
        /// Player book
        /// </summary>
        public BookModeSetting BookMode { get; set; }

        /// <summary>
        /// Gets the book used for manual setting
        /// </summary>
        public Book? Book => BookMode switch {
                BookModeSetting.NoBook  => null,
                BookModeSetting.Unrated => BookUnrated,
                _                       => Book2500,
            };

        /// <summary>
        /// Gets human search mode
        /// </summary>
        /// <param name="difficultyLevel"> Difficulty level</param>
        /// <returns>
        /// Search mode
        /// </returns>
        public string HumanSearchMode(SettingDifficultyLevel difficultyLevel) => GetBoardSearchSetting(difficultyLevel).HumanSearchMode();

        /// <summary>
        /// Gets the chess board setting for the specified level
        /// </summary>
        /// <returns>
        /// Chess board setting for this specified difficulty level
        /// </returns>
        public ChessSearchSetting GetBoardSearchSetting(SettingDifficultyLevel difficultyLevel) {
            ChessSearchSetting retVal;
            SearchOption       searchOption;
            ThreadingMode      threadingMode;
            int                timeOutInSec;

            searchOption  = SearchOption.UseAlphaBeta | SearchOption.UseIterativeDepthSearch | SearchOption.UseTransTable;
            threadingMode = ThreadingMode.OnePerProcessorForSearch;
            timeOutInSec  = 0;
            retVal        = difficultyLevel switch {
                SettingDifficultyLevel.VeryEasy     => new ChessSearchSetting(new SearchEngineSetting(searchOption, threadingMode, searchDepth: 2, timeOutInSec, RandomMode.On, TransTableEntryCount),
                                                                              new BoardEvaluationWeak(),
                                                                              new BoardEvaluationWeak(),
                                                                              BookModeSetting.NoBook,
                                                                              SettingDifficultyLevel.VeryEasy),
                SettingDifficultyLevel.Easy         => new ChessSearchSetting(new SearchEngineSetting(searchOption, threadingMode, searchDepth: 2, timeOutInSec, RandomMode.On, TransTableEntryCount),
                                                                              new BoardEvaluationBasic(),
                                                                              new BoardEvaluationBasic(),
                                                                              BookModeSetting.NoBook,
                                                                              SettingDifficultyLevel.Easy),
                SettingDifficultyLevel.Intermediate => new ChessSearchSetting(new SearchEngineSetting(searchOption, threadingMode, searchDepth: 4, timeOutInSec, RandomMode.On, TransTableEntryCount),
                                                                              new BoardEvaluationBasic(),
                                                                              new BoardEvaluationBasic(),
                                                                              BookModeSetting.Unrated,
                                                                              SettingDifficultyLevel.Intermediate),
                SettingDifficultyLevel.Hard         => new ChessSearchSetting(new SearchEngineSetting(searchOption, threadingMode, searchDepth: 4, timeOutInSec, RandomMode.On, TransTableEntryCount),
                                                                              new BoardEvaluationBasic(),
                                                                              new BoardEvaluationBasic(),
                                                                              BookModeSetting.ELOGT2500,
                                                                              SettingDifficultyLevel.Hard),
                SettingDifficultyLevel.VeryHard     => new ChessSearchSetting(new SearchEngineSetting(searchOption, threadingMode, searchDepth: 6, timeOutInSec, RandomMode.On, TransTableEntryCount),
                                                                              WhiteBoardEvaluator ?? new BoardEvaluationBasic(),
                                                                              BlackBoardEvaluator ?? new BoardEvaluationBasic(),
                                                                              BookModeSetting.ELOGT2500,
                                                                              SettingDifficultyLevel.VeryHard),
                _                                   => this,
            };
            return (retVal);
        }

        /// <summary>
        /// Gets the chess board setting for the current level
        /// </summary>
        /// <returns>
        /// Chess search setting for the current difficulty level
        /// </returns>
        public ChessSearchSetting GetBoardSearchSetting() => GetBoardSearchSetting(DifficultyLevel);

        /// <summary>
        /// Returns the human readable search mode
        /// </summary>
        /// <returns>
        /// Search mode
        /// </returns>
        public override string HumanSearchMode() {
            string bookMode = BookMode switch {
                BookModeSetting.NoBook  => "",
                BookModeSetting.Unrated => " Unrated Book. ",
                _                       => " Ranked Master Book. ",
            };
            return base.HumanSearchMode(bookMode);
        }
    } // Class BoardSearchSetting
}
