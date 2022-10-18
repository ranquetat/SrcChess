using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using GenericSearchEngine;
using SrcChess2.Core;

namespace SrcChess2 {
    /// <summary>
    /// Ask user about search mode
    /// </summary>
    public partial class FrmSearchMode : Window {
        /// <summary>Search mode setting</summary>
        private readonly ChessSearchSetting   m_chessSearchSetting = null!;
        /// <summary>Board evaluation utility class</summary>
        private readonly BoardEvaluationUtil? m_boardEvalUtil;

        /// <summary>
        /// Class Ctor
        /// </summary>
        public FrmSearchMode() => InitializeComponent();

        /// <summary>
        /// Class constructor
        /// </summary>
        /// <param name="chessSearchSetting"> Actual search mode</param>
        /// <param name="boardEvalUtil">      Board Evaluation list</param>
        public FrmSearchMode(ChessSearchSetting chessSearchSetting, BoardEvaluationUtil boardEvalUtil) : this() {
            int pos;
            
            m_chessSearchSetting = chessSearchSetting;
            m_boardEvalUtil      = boardEvalUtil;
            foreach (IBoardEvaluation boardEval in m_boardEvalUtil.BoardEvaluators) {
                pos = comboBoxWhiteBEval.Items.Add(boardEval.Name);
                if (chessSearchSetting.WhiteBoardEvaluator == boardEval) {
                    comboBoxWhiteBEval.SelectedIndex = pos;
                }
                pos = comboBoxBlackBEval.Items.Add(boardEval.Name);
                if (chessSearchSetting.BlackBoardEvaluator == boardEval) {
                    comboBoxBlackBEval.SelectedIndex = pos;
                }
            }
            if (chessSearchSetting.ThreadingMode == ThreadingMode.OnePerProcessorForSearch) {
                radioButtonOnePerProc.IsChecked = true;
            } else if (chessSearchSetting.ThreadingMode == ThreadingMode.DifferentThreadForSearch) {
                radioButtonOneForUI.IsChecked = true;
            } else {
                radioButtonNoThread.IsChecked = true;
            }
            if (chessSearchSetting.BookMode == ChessSearchSetting.BookModeSetting.NoBook) {
                radioButtonNoBook.IsChecked = true;
            } else if (chessSearchSetting.BookMode == ChessSearchSetting.BookModeSetting.Unrated) {
                radioButtonUnrated.IsChecked = true;
            } else {
                radioButtonELO2500.IsChecked = true;
            }
            if ((chessSearchSetting.SearchOption & SearchOption.UseAlphaBeta) != 0) {
                radioButtonAlphaBeta.IsChecked = true;
            } else {
                radioButtonMinMax.IsChecked  = true;
            }
            if (chessSearchSetting.SearchDepth == 0) {
                radioButtonAvgTime.IsChecked = true;
                textBoxTimeInSec.Text        = chessSearchSetting.TimeOutInSec.ToString(CultureInfo.InvariantCulture);
                plyCount.Value               = 6;
            } else {
                if ((chessSearchSetting.SearchOption & SearchOption.UseIterativeDepthSearch) == SearchOption.UseIterativeDepthSearch) {
                    radioButtonFixDepthIterative.IsChecked = true;
                } else {
                    radioButtonFixDepth.IsChecked = true;
                }
                plyCount.Value        = chessSearchSetting.SearchDepth;
                textBoxTimeInSec.Text = "15";
            }
            plyCount2.Content   = plyCount.Value.ToString();
            switch(chessSearchSetting.RandomMode) {
            case RandomMode.Off:
                radioButtonRndOff.IsChecked = true;
                break;
            case RandomMode.OnRepetitive:
                radioButtonRndOnRep.IsChecked = true;
                break;
            default:
                radioButtonRndOn.IsChecked = true;
                break;
            }
            textBoxTransSize.Text  = (chessSearchSetting.TransTableEntryCount / 1000000 * 32).ToString(CultureInfo.InvariantCulture);    // Roughly 32 bytes / entry
            checkBoxTransTable.IsChecked = (chessSearchSetting.SearchOption & SearchOption.UseTransTable) != 0;
            plyCount.ValueChanged += new RoutedPropertyChangedEventHandler<double>(PlyCount_ValueChanged);
        }

        /// <summary>
        /// Called when the ply count is changed
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event parameter</param>
        private void PlyCount_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => plyCount2.Content = plyCount.Value.ToString(CultureInfo.InvariantCulture);

        /// <summary>
        /// Set the plyCount/avgTime control state
        /// </summary>
        private void SetPlyAvgTimeState() {
            if (radioButtonAvgTime.IsChecked == true) {
                plyCount.IsEnabled         = false;
                labelNumberOfPly.IsEnabled = false;
                textBoxTimeInSec.IsEnabled = true;
                labelAvgTime.IsEnabled     = true;
            } else {
                plyCount.IsEnabled         = true;
                labelNumberOfPly.IsEnabled = true;
                textBoxTimeInSec.IsEnabled = false;
                labelAvgTime.IsEnabled     = false;
            }
        }

        /// <summary>
        /// Called when radioButtonFixDepth checked state has been changed
        /// </summary>
        /// <param name="sender"> Sender object</param>
        /// <param name="e">      Event parameter</param>
        private void RadioButtonSearchType_CheckedChanged(object sender, RoutedEventArgs e) => SetPlyAvgTimeState();

        /// <summary>
        /// Called when the time in second textbox changed
        /// </summary>
        /// <param name="sender"> Sender object</param>
        /// <param name="e">      Event parameter</param>
        private void TextBoxTimeInSec_TextChanged(object sender, TextChangedEventArgs e)
            => butOk.IsEnabled = (int.TryParse(textBoxTimeInSec.Text, out int val) && val > 0 && val < 999);

        /// <summary>
        /// Called when the transposition table size is changed
        /// </summary>
        /// <param name="sender"> Sender object</param>
        /// <param name="e">      Event parameter</param>
        private void TextBoxTransSize_TextChanged(object sender, TextChangedEventArgs e)
            => butOk.IsEnabled = (int.TryParse(textBoxTransSize.Text, out int val) && val > 4 && val < 1000);

        /// <summary>
        /// Update the SearchMode object
        /// </summary>
        private void UpdateSearchMode() {
            int               transTableSize;
            IBoardEvaluation? boardEval;

            m_chessSearchSetting.SearchOption = (radioButtonAlphaBeta.IsChecked == true) ? SearchOption.UseAlphaBeta : SearchOption.UseMinMax;
            if (radioButtonNoBook.IsChecked == true) {
                m_chessSearchSetting.BookMode = ChessSearchSetting.BookModeSetting.NoBook;
            } else if (radioButtonUnrated.IsChecked == true) {
                m_chessSearchSetting.BookMode = ChessSearchSetting.BookModeSetting.Unrated;
            } else {
                m_chessSearchSetting.BookMode = ChessSearchSetting.BookModeSetting.ELOGT2500;
            }
            if (checkBoxTransTable.IsChecked == true) {
                m_chessSearchSetting.SearchOption |= SearchOption.UseTransTable;
            }
            if (radioButtonOnePerProc.IsChecked == true) {
                m_chessSearchSetting.ThreadingMode = ThreadingMode.OnePerProcessorForSearch;
            } else if (radioButtonOneForUI.IsChecked == true) {
                m_chessSearchSetting.ThreadingMode = ThreadingMode.DifferentThreadForSearch;
            } else {
                m_chessSearchSetting.ThreadingMode = ThreadingMode.Off;
            }
            if (radioButtonAvgTime.IsChecked == true) {
                m_chessSearchSetting.SearchDepth  = 0;
                m_chessSearchSetting.TimeOutInSec = int.Parse(textBoxTimeInSec.Text);
            } else {
                m_chessSearchSetting.SearchDepth  = (int)plyCount.Value;
                m_chessSearchSetting.TimeOutInSec = 0;
                if (radioButtonFixDepthIterative.IsChecked == true) {
                    m_chessSearchSetting.SearchOption |= SearchOption.UseIterativeDepthSearch;
                }
            }
            if (radioButtonRndOff.IsChecked == true) {
                m_chessSearchSetting.RandomMode = RandomMode.Off;
            } else if (radioButtonRndOnRep.IsChecked == true) {
                m_chessSearchSetting.RandomMode = RandomMode.OnRepetitive;
            } else {
                m_chessSearchSetting.RandomMode = RandomMode.On;
            }
            transTableSize                            = int.Parse(textBoxTransSize.Text);
            m_chessSearchSetting.TransTableEntryCount = transTableSize / 32 * 1000000;
            boardEval                                 = m_boardEvalUtil!.FindBoardEvaluator(comboBoxWhiteBEval.SelectedItem.ToString());
            boardEval                               ??= m_boardEvalUtil.BoardEvaluators[0];
            m_chessSearchSetting.WhiteBoardEvaluator  = boardEval;
            boardEval                                 = m_boardEvalUtil.FindBoardEvaluator(comboBoxBlackBEval.SelectedItem.ToString());
            boardEval                               ??= m_boardEvalUtil.BoardEvaluators[0];
            m_chessSearchSetting.BlackBoardEvaluator  = boardEval;
        }

        /// <summary>
        /// Called when the Ok button is clicked
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event parameter</param>
        private void ButOk_Click(object sender, RoutedEventArgs e) {
            UpdateSearchMode();
            DialogResult = true;
            Close();
        }
    } // Class FrmManualSearchMode
} // Namespace
