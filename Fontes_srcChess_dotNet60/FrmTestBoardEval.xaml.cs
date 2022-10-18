using System;
using System.Globalization;
using System.Windows;
using GenericSearchEngine;
using SrcChess2.Core;

namespace SrcChess2 {
    /// <summary>
    /// Enter parameters for testing the board evaluation functions
    /// </summary>
    public partial class FrmTestBoardEval : Window {
        /// <summary>Board evaluation utility</summary>
        private readonly BoardEvaluationUtil? m_boardEvalUtil;

        /// <summary>Resulting search mode</summary>
        private readonly ChessSearchSetting?  m_chessSearchSetting;
        
        /// <summary>
        /// Class Ctor
        /// </summary>
        public FrmTestBoardEval() => InitializeComponent();

        /// <summary>
        /// Class Ctor
        /// </summary>
        /// <param name="boardEvalUtil">        Board evaluation utility class</param>
        /// <param name="chessSearchSetting">   Search mode template</param>
        public FrmTestBoardEval(BoardEvaluationUtil boardEvalUtil, ChessSearchSetting chessSearchSetting) : this() {
            m_chessSearchSetting = new ChessSearchSetting(SearchOption.UseAlphaBeta,
                                                          chessSearchSetting.ThreadingMode,
                                                          searchDepth: 4,
                                                          timeOutInSec: 0,
                                                          chessSearchSetting.RandomMode,
                                                          transTableEntryCount: 0,
                                                          boardEvalUtil.BoardEvaluators[0],
                                                          boardEvalUtil.BoardEvaluators[0],
                                                          ChessSearchSetting.BookModeSetting.NoBook,
                                                          ChessSearchSetting.SettingDifficultyLevel.Manual);
            foreach (IBoardEvaluation boardEval in boardEvalUtil.BoardEvaluators) {
                comboBoxWhiteBEval.Items.Add(boardEval.Name);
                comboBoxBlackBEval.Items.Add(boardEval.Name);
            }
            comboBoxWhiteBEval.SelectedIndex = 0;
            comboBoxBlackBEval.SelectedIndex = (comboBoxBlackBEval.Items.Count == 0) ? 0 : 1;
            m_boardEvalUtil                  = boardEvalUtil;
            plyCount2.Content                = plyCount.Value.ToString();
            gameCount2.Content               = gameCount.Value.ToString();
            plyCount.ValueChanged           += new RoutedPropertyChangedEventHandler<double>(PlyCount_ValueChanged);
            gameCount.ValueChanged          += new RoutedPropertyChangedEventHandler<double>(GameCount_ValueChanged);
        }

        /// <summary>
        /// Called when game count changed
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event parameter</param>
        private void GameCount_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
            => gameCount2.Content  = ((int)gameCount.Value).ToString(CultureInfo.InvariantCulture);

        /// <summary>
        /// Called when ply count changed
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event parameter</param>
        private void PlyCount_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
            => plyCount2.Content   = plyCount.Value.ToString(CultureInfo.InvariantCulture);

        /// <summary>
        /// Get the chess search setting
        /// </summary>
        public ChessSearchSetting ChessSearchSetting {
            get {
                IBoardEvaluation? boardEval;
                
                boardEval = m_boardEvalUtil!.FindBoardEvaluator(comboBoxWhiteBEval.SelectedItem.ToString());
                if (boardEval == null) {
                    boardEval = m_boardEvalUtil.BoardEvaluators[0];
                }
                m_chessSearchSetting!.WhiteBoardEvaluator = boardEval;
                boardEval = m_boardEvalUtil.FindBoardEvaluator(comboBoxBlackBEval.SelectedItem.ToString());
                if (boardEval == null) {
                    boardEval = m_boardEvalUtil.BoardEvaluators[0];
                }
                m_chessSearchSetting.BlackBoardEvaluator = boardEval;
                m_chessSearchSetting.SearchDepth         = (int)plyCount.Value;
                return m_chessSearchSetting;
            }
        }

        /// <summary>
        /// Get the number of games to test
        /// </summary>
        public int GameCount => (int)gameCount.Value;

        /// <summary>
        /// Called when the ok button is pressed
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event parameter</param>
        private void ButOk_Click(object sender, RoutedEventArgs e) {
            DialogResult = true;
            Close();
        }
    }
}
