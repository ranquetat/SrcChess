using System;
using System.Collections.Generic;
using System.Windows.Media;
using SrcChess2.FicsInterface;
using System.Windows;
using GenericSearchEngine;
using SrcChess2.Core;

namespace SrcChess2 {

    /// <summary>
    /// Transfer object setting from/to the properties setting
    /// </summary>
    internal class SettingAdaptor {

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="settings"> Properties setting</param>
        public SettingAdaptor(Properties.Settings settings) => Settings = settings;

        /// <summary>
        /// Settings
        /// </summary>
        public Properties.Settings Settings { get; private set; }

        /// <summary>
        /// Convert a color name to a color
        /// </summary>
        /// <param name="colorName"> Name of the color or hexa representation of the color</param>
        /// <returns>
        /// Color
        /// </returns>
        private static Color NameToColor(string colorName) {
            Color   retVal;

            if (colorName.Length == 8 && (Char.IsLower(colorName[0]) || Char.IsDigit(colorName[0])) &&
                int.TryParse(colorName, System.Globalization.NumberStyles.HexNumber, null, out int val)) {
                retVal = Color.FromArgb((byte)((val >> 24) & 255), (byte)((val >> 16) & 255), (byte)((val >> 8) & 255), (byte)(val & 255));
            } else {
                retVal = (Color)ColorConverter.ConvertFromString(colorName);
            }
            return (retVal);    
        }

        /// <summary>
        /// Load the FICS connection setting from the properties setting
        /// </summary>
        /// <param name="ficsSetting"> FICS connection setting</param>
        public void LoadFicsConnectionSetting(FicsConnectionSetting ficsSetting) {
            ficsSetting.HostName  = Settings.FICSHostName;
            ficsSetting.HostPort  = Settings.FICSHostPort;
            ficsSetting.UserName  = Settings.FICSUserName;
            ficsSetting.Anonymous = string.Compare(Settings.FICSUserName, "guest", true) == 0;
        }

        /// <summary>
        /// Save the connection settings to the property setting
        /// </summary>
        /// <param name="ficsSetting">  Copy the FICS connection setting to the properties setting</param>
        public void SaveFicsConnectionSetting(FicsConnectionSetting ficsSetting) {
            Settings.FICSHostName = ficsSetting.HostName;
            Settings.FICSHostPort = ficsSetting.HostPort;
            Settings.FICSUserName = ficsSetting.Anonymous ? "Guest" : ficsSetting.UserName;
        }

        /// <summary>
        /// Load the chess board control settings from the property setting
        /// </summary>
        /// <param name="chessCtl"> Chess board control</param>
        public void LoadChessBoardCtl(ChessBoardControl chessCtl) {
            chessCtl.LiteCellColor   = NameToColor(Settings.LiteCellColor);
            chessCtl.DarkCellColor   = NameToColor(Settings.DarkCellColor);
            chessCtl.WhitePieceColor = NameToColor(Settings.WhitePieceColor);
            chessCtl.BlackPieceColor = NameToColor(Settings.BlackPieceColor);
            chessCtl.MoveFlashing    = Settings.FlashPiece;
        }

        /// <summary>
        /// Save the chess board control settings to the property setting
        /// </summary>
        /// <param name="chessCtl"> Chess board control</param>
        public void SaveChessBoardCtl(ChessBoardControl chessCtl) {
            Settings.WhitePieceColor = chessCtl.WhitePieceColor.ToString();
            Settings.BlackPieceColor = chessCtl.BlackPieceColor.ToString();
            Settings.LiteCellColor   = chessCtl.LiteCellColor.ToString();
            Settings.DarkCellColor   = chessCtl.DarkCellColor.ToString();
            Settings.FlashPiece      = chessCtl.MoveFlashing;
        }

        /// <summary>
        /// Load main window settings from the property setting
        /// </summary>
        /// <param name="mainWnd">      Main window</param>
        /// <param name="pieceSetList"> List of available piece sets</param>
        public void LoadMainWindow(MainWindow mainWnd, SortedList<string,PieceSet> pieceSetList) {
            mainWnd.m_colorBackground = NameToColor(Settings.BackgroundColor);
            mainWnd.Background        = new SolidColorBrush(mainWnd.m_colorBackground);
            mainWnd.PieceSet          = pieceSetList[Settings.PieceSet];
            if (!Enum.TryParse(Settings.WndState, out WindowState windowState)) {
                windowState = WindowState.Normal;
            }
            mainWnd.WindowState = windowState;
            mainWnd.Height      = Settings.WndHeight;
            mainWnd.Width       = Settings.WndWidth;
            if (!double.IsNaN(Settings.WndLeft)) {
                mainWnd.Left = Settings.WndLeft;
            }
            if (!double.IsNaN(Settings.WndTop)) {
                mainWnd.Top = Settings.WndTop;
            }
            mainWnd.m_puzzleMasks[0] = Settings.PuzzleDoneLow;
            mainWnd.m_puzzleMasks[1] = Settings.PuzzleDoneHigh;
        }

        /// <summary>
        /// Save main window settings from the property setting
        /// </summary>
        /// <param name="mainWnd"> Main window</param>
        public void SaveMainWindow(MainWindow mainWnd) {
            Settings.BackgroundColor = mainWnd.m_colorBackground.ToString();
            Settings.PieceSet        = mainWnd.PieceSet?.Name ?? "???";
            Settings.WndState        = mainWnd.WindowState.ToString();
            Settings.WndHeight       = mainWnd.Height;
            Settings.WndWidth        = mainWnd.Width;
            Settings.WndLeft         = mainWnd.Left;
            Settings.WndTop          = mainWnd.Top;
            Settings.PuzzleDoneLow   = mainWnd.m_puzzleMasks[0];
            Settings.PuzzleDoneHigh  = mainWnd.m_puzzleMasks[1];
        }

        /// <summary>
        /// Save the chess board control settings to the property setting
        /// </summary>
        /// <param name="chessCtl"> Chess board control</param>
        public void FromChessBoardCtl(ChessBoardControl chessCtl) {
            Settings.WhitePieceColor = chessCtl.WhitePieceColor.ToString();
            Settings.BlackPieceColor = chessCtl.BlackPieceColor.ToString();
            Settings.LiteCellColor   = chessCtl.LiteCellColor.ToString();
            Settings.DarkCellColor   = chessCtl.DarkCellColor.ToString();
        }

        /// <summary>
        /// Load search setting from property settings
        /// </summary>
        /// <param name="boardEvalUtil">      Board evaluation utility</param>
        /// <param name="chessSearchSetting"> Chess search setting</param>
        public void LoadSearchMode(BoardEvaluationUtil boardEvalUtil, out ChessSearchSetting chessSearchSetting) {
            int                                       transTableSize;
            int                                       transTableEntryCount;
            SearchEngineSetting                       searchEngineOptions;
            ChessSearchSetting.SettingDifficultyLevel difficultyLevel;
            SearchOption                              searchOption;
            ThreadingMode                             threadingMode;
            ChessSearchSetting.BookModeSetting        bookMode;
            IBoardEvaluation                          whiteBoardEvaluation;
            IBoardEvaluation                          blackBoardEvaluation;
            int                                       searchDepth;
            int                                       timeOutInSec;
            RandomMode                                randomMode;

            transTableSize = Settings.TransTableSize;
            if (transTableSize < 5) {
                transTableSize = 5;
            } else if (transTableSize > 1000) {
                transTableSize = 1000;
            }
            transTableEntryCount = transTableSize / 32 * 1000000;
            searchOption         = Settings.UseAlphaBeta ? SearchOption.UseAlphaBeta : SearchOption.UseMinMax;
            if (Settings.UseTransTable) {
                searchOption |= SearchOption.UseTransTable;
            }
            if (Settings.UsePlyCountIterative) {
                searchOption |= SearchOption.UseIterativeDepthSearch;
            }
            threadingMode = Settings.UseThread switch {
                    0 => ThreadingMode.Off,
                    1 => ThreadingMode.DifferentThreadForSearch,
                    _ => ThreadingMode.OnePerProcessorForSearch
                };
            difficultyLevel = Settings.DifficultyLevel >= 0 && Settings.DifficultyLevel < 6 ? (ChessSearchSetting.SettingDifficultyLevel)Settings.DifficultyLevel : ChessSearchSetting.SettingDifficultyLevel.Manual;
            bookMode        = ((ChessSearchSetting.BookModeSetting)Settings.BookType) switch {
                                    ChessSearchSetting.BookModeSetting.NoBook or ChessSearchSetting.BookModeSetting.Unrated => (ChessSearchSetting.BookModeSetting)Settings.BookType,
                                    _                                                                                       => ChessSearchSetting.BookModeSetting.ELOGT2500
                                };
            whiteBoardEvaluation = boardEvalUtil.FindBoardEvaluator(Settings.WhiteBoardEval) ?? boardEvalUtil.BoardEvaluators[0];
            blackBoardEvaluation = boardEvalUtil.FindBoardEvaluator(Settings.BlackBoardEval) ?? boardEvalUtil.BoardEvaluators[0];
            searchDepth          = Settings.UsePlyCount | Settings.UsePlyCountIterative ? ((Settings.PlyCount > 1 && Settings.PlyCount < 12) ? Settings.PlyCount : 6) : 0;
            timeOutInSec         = Settings.UsePlyCount | Settings.UsePlyCountIterative ? 0 : (Settings.AverageTime > 0 && Settings.AverageTime < 1000) ? Settings.AverageTime : 15;
            randomMode           = (Settings.RandomMode >= 0 && Settings.RandomMode <= 2) ? (RandomMode)Settings.RandomMode : RandomMode.On;
            searchEngineOptions  = new SearchEngineSetting(searchOption, threadingMode, searchDepth, timeOutInSec, randomMode, transTableEntryCount);
            chessSearchSetting   = new ChessSearchSetting(searchEngineOptions, whiteBoardEvaluation, blackBoardEvaluation, bookMode, difficultyLevel);
        }

        /// <summary>
        /// Save the search mode to properties setting
        /// </summary>
        /// <param name="chessBoardSetting"> Chess board setting</param>
        public void SaveSearchMode(ChessSearchSetting chessSearchSetting) {
            SearchOption searchOption = chessSearchSetting.SearchOption; 

            Settings.UseAlphaBeta         = (searchOption & SearchOption.UseAlphaBeta)  != 0;
            Settings.UseTransTable        = (searchOption & SearchOption.UseTransTable) != 0;
            Settings.UsePlyCountIterative = (searchOption & SearchOption.UseIterativeDepthSearch) != 0;
            Settings.UsePlyCount          = (searchOption & SearchOption.UseIterativeDepthSearch) == 0 && chessSearchSetting.SearchDepth != 0;
            Settings.DifficultyLevel      = (chessSearchSetting.DifficultyLevel == ChessSearchSetting.SettingDifficultyLevel.Manual) ? 0 : (int)chessSearchSetting.DifficultyLevel;
            Settings.PlyCount             = chessSearchSetting.SearchDepth;
            Settings.AverageTime          = chessSearchSetting.TimeOutInSec;
            Settings.BookType             = (int)chessSearchSetting.BookMode;
            Settings.UseThread            = (int)chessSearchSetting.ThreadingMode;
            Settings.RandomMode           = (int)chessSearchSetting.RandomMode;
            Settings.TransTableSize       = chessSearchSetting.TransTableEntryCount * 32 / 1000000;
            Settings.WhiteBoardEval       = chessSearchSetting.WhiteBoardEvaluator?.Name ?? "???";
            Settings.BlackBoardEval       = chessSearchSetting.BlackBoardEvaluator?.Name ?? "???";
        }

        /// <summary>
        /// Load move viewer setting from properties setting
        /// </summary>
        /// <param name="moveViewer">   Move viewer</param>
        public void LoadMoveViewer(MoveViewer moveViewer) => moveViewer.DisplayMode  = (Settings.MoveNotation == 0) ? MoveViewer.ViewerDisplayMode.MovePos : MoveViewer.ViewerDisplayMode.Pgn;

        /// <summary>
        /// Save move viewer setting to properties setting
        /// </summary>
        /// <param name="moveViewer">   Move viewer</param>
        public void SaveMoveViewer(MoveViewer moveViewer) => Settings.MoveNotation = (moveViewer.DisplayMode == MoveViewer.ViewerDisplayMode.MovePos) ? 0 : 1;

        /// <summary>
        /// Load FICS search criteria from properties setting
        /// </summary>
        /// <param name="searchCriteria">   Search criteria</param>
        public void LoadFICSSearchCriteria(SearchCriteria searchCriteria) {
            searchCriteria.PlayerName        = Settings.FICSSPlayerName;
            searchCriteria.BlitzGame         = Settings.FICSSBlitz;
            searchCriteria.LightningGame     = Settings.FICSSLightning;
            searchCriteria.UntimedGame       = Settings.FICSSUntimed;
            searchCriteria.StandardGame      = Settings.FICSSStandard;
            searchCriteria.IsRated           = Settings.FICSSRated;
            searchCriteria.MinRating         = SearchCriteria.CnvToNullableIntValue(Settings.FICSSMinRating);
            searchCriteria.MinTimePerPlayer  = SearchCriteria.CnvToNullableIntValue(Settings.FICSSMinTimePerPlayer);
            searchCriteria.MaxTimePerPlayer  = SearchCriteria.CnvToNullableIntValue(Settings.FICSSMaxTimePerPlayer);
            searchCriteria.MinIncTimePerMove = SearchCriteria.CnvToNullableIntValue(Settings.FICSSMinIncTimePerMove);
            searchCriteria.MaxIncTimePerMove = SearchCriteria.CnvToNullableIntValue(Settings.FICSSMaxIncTimePerMove);
            searchCriteria.MaxMoveDone       = Settings.FICSSMaxMoveDone;
            searchCriteria.MoveTimeOut       = SearchCriteria.CnvToNullableIntValue(Settings.FICSMoveTimeOut);
        }

        /// <summary>
        /// Save FICS search criteria to properties setting
        /// </summary>
        /// <param name="searchCriteria">   Search criteria</param>
        public void SaveFicsSearchCriteria(SearchCriteria searchCriteria) {
            Settings.FICSSPlayerName        = searchCriteria.PlayerName;
            Settings.FICSSBlitz             = searchCriteria.BlitzGame;
            Settings.FICSSLightning         = searchCriteria.LightningGame;
            Settings.FICSSUntimed           = searchCriteria.UntimedGame;
            Settings.FICSSStandard          = searchCriteria.StandardGame;
            Settings.FICSSRated             = searchCriteria.IsRated;
            Settings.FICSSMinRating         = searchCriteria.MinRating.ToString();
            Settings.FICSSMinTimePerPlayer  = searchCriteria.MinTimePerPlayer.ToString();
            Settings.FICSSMaxTimePerPlayer  = searchCriteria.MaxTimePerPlayer.ToString();
            Settings.FICSSMinIncTimePerMove = searchCriteria.MinIncTimePerMove.ToString();
            Settings.FICSSMaxIncTimePerMove = searchCriteria.MaxIncTimePerMove.ToString();
            Settings.FICSSMaxMoveDone       = searchCriteria.MaxMoveDone;
            Settings.FICSMoveTimeOut        = searchCriteria.MoveTimeOut.ToString();
        }
    }
}
