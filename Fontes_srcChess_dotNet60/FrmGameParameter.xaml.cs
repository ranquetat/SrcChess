using System.Windows;
using SrcChess2.Core;

namespace SrcChess2 {
    /// <summary>
    /// Pickup Game Parameter from the player
    /// </summary>
    public partial class FrmGameParameter : Window {
        /// <summary>Parent Window</summary>
        private readonly MainWindow          m_parentWindow = null!;
        /// <summary>Utility class to handle board evaluation objects</summary>
        private readonly BoardEvaluationUtil m_boardEvalUtil = null!;
        /// <summary>Search mode</summary>
        private readonly ChessSearchSetting  m_chessSearchSetting = null!;

        /// <summary>
        /// Class Ctor
        /// </summary>
        public FrmGameParameter() => InitializeComponent();

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="parent">             Parent Window</param>
        /// <param name="chessSearchSetting"> Chess search setting</param>
        /// <param name="boardEvalUtil">      Utility class to handle board evaluation objects</param>
        private FrmGameParameter(MainWindow parent, ChessSearchSetting chessSearchSetting, BoardEvaluationUtil boardEvalUtil) : this() {
            m_parentWindow       = parent;
            m_chessSearchSetting = chessSearchSetting;
            m_boardEvalUtil      = boardEvalUtil;
            switch(m_parentWindow.PlayingMode) {
            case MainWindow.MainPlayingMode.DesignMode:
                throw new System.ApplicationException("Must not be called in design mode.");
            case MainWindow.MainPlayingMode.ComputerPlayWhite:
            case MainWindow.MainPlayingMode.ComputerPlayBlack:
                radioButtonPlayerAgainstComputer.IsChecked = true;
                radioButtonPlayerAgainstComputer.Focus();
                break;
            case MainWindow.MainPlayingMode.PlayerAgainstPlayer:
                radioButtonPlayerAgainstPlayer.IsChecked = true;
                radioButtonPlayerAgainstPlayer.Focus();
                break;
            case MainWindow.MainPlayingMode.ComputerPlayBoth:
                radioButtonComputerAgainstComputer.IsChecked = true;
                radioButtonComputerAgainstComputer.Focus();
                break;
            }
            if (m_parentWindow.PlayingMode == MainWindow.MainPlayingMode.ComputerPlayBlack) { 
                radioButtonComputerPlayBlack.IsChecked = true;
            } else {
                radioButtonComputerPlayWhite.IsChecked = true;
            }
            switch (m_chessSearchSetting.DifficultyLevel) {
            case ChessSearchSetting.SettingDifficultyLevel.Manual:
                radioButtonLevelManual.IsChecked = true;
                break;
            case ChessSearchSetting.SettingDifficultyLevel.VeryEasy:
                radioButtonLevel1.IsChecked = true;
                break;
            case ChessSearchSetting.SettingDifficultyLevel.Easy:
                radioButtonLevel2.IsChecked = true;
                break;
            case ChessSearchSetting.SettingDifficultyLevel.Intermediate:
                radioButtonLevel3.IsChecked = true;
                break;
            case ChessSearchSetting.SettingDifficultyLevel.Hard:
                radioButtonLevel4.IsChecked = true;
                break;
            case ChessSearchSetting.SettingDifficultyLevel.VeryHard:
                radioButtonLevel5.IsChecked = true;
                break;
            default:
                radioButtonLevel1.IsChecked = true;
                break;
            }
            CheckState();
            radioButtonLevel1.ToolTip      = chessSearchSetting.HumanSearchMode(ChessSearchSetting.SettingDifficultyLevel.VeryEasy);
            radioButtonLevel2.ToolTip      = chessSearchSetting.HumanSearchMode(ChessSearchSetting.SettingDifficultyLevel.Easy);
            radioButtonLevel3.ToolTip      = chessSearchSetting.HumanSearchMode(ChessSearchSetting.SettingDifficultyLevel.Intermediate);
            radioButtonLevel4.ToolTip      = chessSearchSetting.HumanSearchMode(ChessSearchSetting.SettingDifficultyLevel.Hard);
            radioButtonLevel5.ToolTip      = chessSearchSetting.HumanSearchMode(ChessSearchSetting.SettingDifficultyLevel.VeryHard);
            radioButtonLevelManual.ToolTip = chessSearchSetting.HumanSearchMode(ChessSearchSetting.SettingDifficultyLevel.Manual);
        }

        /// <summary>
        /// Check the state of the group box
        /// </summary>
        private void CheckState() {
            groupBoxComputerPlay.IsEnabled = radioButtonPlayerAgainstComputer.IsChecked!.Value;
            butUpdManual.IsEnabled         = radioButtonLevelManual.IsChecked == true;
        }

        /// <summary>
        /// Called to accept the form
        /// </summary>
        /// <param name="sender"> Sender object</param>
        /// <param name="e">      Event arguments</param>
        private void ButOk_Click(object sender, RoutedEventArgs e) {
            if (radioButtonPlayerAgainstComputer.IsChecked == true) {
                m_parentWindow!.PlayingMode = (radioButtonComputerPlayBlack.IsChecked == true) ? MainWindow.MainPlayingMode.ComputerPlayBlack : MainWindow.MainPlayingMode.ComputerPlayWhite;
            } else if (radioButtonPlayerAgainstPlayer.IsChecked == true) {
                m_parentWindow!.PlayingMode = MainWindow.MainPlayingMode.PlayerAgainstPlayer;
            } else if (radioButtonComputerAgainstComputer.IsChecked == true) {
                m_parentWindow!.PlayingMode = MainWindow.MainPlayingMode.ComputerPlayBoth;
            }
            DialogResult = true;
            Close();
        }

        /// <summary>
        /// Called to open the manual setting
        /// </summary>
        /// <param name="sender"> Sender object</param>
        /// <param name="e">      Event arguments</param>
        private void ButUpdManual_Click(object sender, RoutedEventArgs e) {
            FrmSearchMode frm;

            frm = new(m_chessSearchSetting, m_boardEvalUtil) {
                Owner = this
            };
            if (frm.ShowDialog() == true) {
                m_parentWindow!.SetSearchMode(m_chessSearchSetting.GetBoardSearchSetting());
            }
        }

        /// <summary>
        /// Called when the radio button value is changed
        /// </summary>
        /// <param name="sender"> Sender object</param>
        /// <param name="e">      Event arguments</param>
        private void RadioButtonOpponent_CheckedChanged(object sender, RoutedEventArgs e) => CheckState();

        /// <summary>
        /// Called when the radio button value is changed
        /// </summary>
        /// <param name="sender"> Sender object</param>
        /// <param name="e">      Event arguments</param>
        private void RadioButtonLevelManual_CheckedChanged(object sender, RoutedEventArgs e) => CheckState();

        /// <summary>
        /// Ask for the game parameter
        /// </summary>
        /// <param name="parent">             Parent window</param>
        /// <param name="chessSearchSetting"> Chess search setting</param>
        /// <param name="boardEvalUtil">      Utility class to handle board evaluation objects</param>
        /// <returns>
        /// true if succeed
        /// </returns>
        public static bool AskGameParameter(MainWindow parent, ChessSearchSetting chessSearchSetting, BoardEvaluationUtil boardEvalUtil) {
            bool             retVal;
            FrmGameParameter frm;

            frm = new(parent, chessSearchSetting, boardEvalUtil) {
                Owner = parent
            };
            retVal     = (frm.ShowDialog() == true);
            if (retVal) {                
                if (frm.radioButtonLevel1.IsChecked == true) {
                    frm.m_chessSearchSetting.DifficultyLevel = ChessSearchSetting.SettingDifficultyLevel.VeryEasy;
                } else if (frm.radioButtonLevel2.IsChecked == true) {
                    frm.m_chessSearchSetting.DifficultyLevel = ChessSearchSetting.SettingDifficultyLevel.Easy;
                } else if (frm.radioButtonLevel3.IsChecked == true) {
                    frm.m_chessSearchSetting.DifficultyLevel = ChessSearchSetting.SettingDifficultyLevel.Intermediate;
                } else if (frm.radioButtonLevel4.IsChecked == true) {
                    frm.m_chessSearchSetting.DifficultyLevel = ChessSearchSetting.SettingDifficultyLevel.Hard;
                } else if (frm.radioButtonLevel5.IsChecked == true) {
                    frm.m_chessSearchSetting.DifficultyLevel = ChessSearchSetting.SettingDifficultyLevel.VeryHard;
                } else if (frm.radioButtonLevelManual.IsChecked == true) {
                    frm.m_chessSearchSetting.DifficultyLevel = ChessSearchSetting.SettingDifficultyLevel.Manual;
                }
                frm.m_parentWindow!.SetSearchMode(frm.m_chessSearchSetting.GetBoardSearchSetting());
            }
            return retVal;
        }

    } // Class FrmGameParameter
} // Namespace
