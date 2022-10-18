using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using SrcChess2.PgnParsing;

namespace SrcChess2 {
    /// <summary>
    /// Interaction logic for frmPgnFilter.xaml
    /// </summary>
    public partial class FrmPgnFilter : Window {
        
        /// <summary>Represent an ELO range in the checked list control</summary>
        private class RangeItem {
            public int             Range { get; set; }
            public                 RangeItem(int range) { Range = range; }
            public override string ToString() =>$"Range {Range} - {Range + 99}";
        }
        
        /// <summary>Clause use to filter PGN games</summary>
        public PgnUtil.FilterClause     m_filterClause;
        /// <summary>PGN Parser</summary>
        private readonly PgnParser?     m_pgnParser;
        /// <summary>PGN games without move list</summary>
        private readonly List<PgnGame>? m_pgnGames;

        /// <summary>
        /// Class Ctor
        /// </summary>
        public FrmPgnFilter() {
            InitializeComponent();
            m_filterClause = new PgnUtil.FilterClause();
        }

        /// <summary>
        /// Class constructor
        /// </summary>
        /// <param name="pgnParser">   PGN parser</param>
        /// <param name="pgnGames">    Raw games</param>
        /// <param name="minElo">      Minimum ELO in the PGN file</param>
        /// <param name="maxElo">      Maximum ELO in the PGN file</param>
        /// <param name="players">     List of players found in the PGN file</param>
        /// <param name="inpFileName"> Name of the input file.</param>
        public FrmPgnFilter(PgnParser pgnParser, List<PgnGame> pgnGames, int minElo, int maxElo, string[] players, string inpFileName) : this() {
            CheckBox checkBox;

            m_pgnParser = pgnParser;
            m_pgnGames  = pgnGames;
            minElo      = minElo / 100 * 100;
            listBoxRange.Items.Clear();
            for (int i = minElo; i < maxElo; i += 100) {
                checkBox = new CheckBox {
                    Content   = new RangeItem(i),
                    IsChecked = true
                };
                listBoxRange.Items.Add(checkBox);
            }
            listBoxPlayer.Items.Clear();
            foreach (string strPlayer in players) {
                checkBox = new CheckBox {
                    Content   = strPlayer,
                    IsChecked = true
                };
                listBoxPlayer.Items.Add(checkBox);
            }
            listBoxEnding.Items.Clear();
            checkBox = new CheckBox {
                Content   = "White Win",
                IsChecked = true
            };
            listBoxEnding.Items.Add(checkBox);
            checkBox = new CheckBox {
                Content   = "Black Win",
                IsChecked = true
            };
            listBoxEnding.Items.Add(checkBox);
            checkBox = new CheckBox {
                Content   = "Draws",
                IsChecked = true
            };
            listBoxEnding.Items.Add(checkBox);
            checkBoxAllRanges.IsChecked  = true;
            checkBoxAllPlayer.IsChecked  = true;
            checkBoxAllEndGame.IsChecked = true;
            listBoxPlayer.IsEnabled      = false;
            listBoxRange.IsEnabled       = false;
            listBoxEnding.IsEnabled      = false;
            labelDesc.Content            = $"{pgnGames.Count} games found in the file '{inpFileName}'";
        }

        /// <summary>
        /// Checks or unchecks all items in a checked list control
        /// </summary>
        /// <param name="listBox">   Control</param>
        /// <param name="isChecked"> true to check, false to uncheck</param>
        private static void CheckAllItems(ListBox listBox, bool isChecked) {
            foreach (CheckBox checkBox in listBox.Items.OfType<CheckBox>()) {
                checkBox.IsChecked  = isChecked;
            }
        }

        /// <summary>
        /// Gets the number of checked item
        /// </summary>
        /// <param name="listBox">      Control</param>
        private static int GetCheckedCount(ListBox listBox) => listBox.Items.OfType<CheckBox>().Count(x => (x.IsChecked == true));

        /// <summary>
        /// Gets and validates information coming from the user
        /// </summary>
        /// <returns>
        /// true if validation is ok, false if not
        /// </returns>
        private bool SyncInfo() {
            bool      retVal = true;
            int       rangeCheckedCount;
            int       playerCheckedCount;
            int       endGameCount;
            RangeItem rangeItem;

            rangeCheckedCount = GetCheckedCount(listBoxRange);
            if (checkBoxAllRanges.IsChecked == true || rangeCheckedCount == listBoxRange.Items.Count) {
                m_filterClause.IsAllRanges = true;
                m_filterClause.HashRanges  = null;
            } else {
                m_filterClause.IsAllRanges = false;
                if (rangeCheckedCount == 0 && checkBoxIncludeUnrated.IsChecked == false) {
                    MessageBox.Show("At least one range must be selected.");
                    retVal = false;
                } else {
                    m_filterClause.HashRanges = new Dictionary<int,int>(rangeCheckedCount);
                    foreach (CheckBox checkBox in listBoxRange.Items.OfType<CheckBox>().Where(x => x.IsChecked == true)) {
                        rangeItem = (RangeItem)checkBox.Content;
                        m_filterClause.HashRanges.Add(rangeItem.Range, 0);
                    }
                }
            }
            playerCheckedCount = GetCheckedCount(listBoxPlayer);
            m_filterClause.IncludesUnrated = checkBoxIncludeUnrated.IsChecked!.Value;
            if (checkBoxAllPlayer.IsChecked == true || playerCheckedCount == listBoxPlayer.Items.Count) {
                m_filterClause.IncludeAllPlayers = true;
                m_filterClause.HashPlayerList    = null;
            } else {
                m_filterClause.IncludeAllPlayers = false;
                if (playerCheckedCount == 0) {
                    MessageBox.Show("At least one player must be selected.");
                    retVal = false;
                } else {
                    m_filterClause.HashPlayerList = new Dictionary<string,string?>(playerCheckedCount);
                    foreach (CheckBox checkBox in listBoxPlayer.Items.OfType<CheckBox>().Where(x => x.IsChecked == true)) {
                        m_filterClause.HashPlayerList.Add((string)checkBox.Content, null);
                    }
                }
            }
            endGameCount = GetCheckedCount(listBoxEnding);
            if (checkBoxAllEndGame.IsChecked == true || endGameCount == listBoxEnding.Items.Count) {
                m_filterClause.IncludeAllEnding          = true;
                m_filterClause.IncludeWhiteWinningEnding = true;
                m_filterClause.IncludeBlackWinningEnding = true;
                m_filterClause.IncludeDrawEnding         = true;
            } else {
                m_filterClause.IncludeAllEnding          = false;
                if (endGameCount == 0) {
                    MessageBox.Show("At least one ending must be selected.");
                    retVal = false;
                } else {
                    m_filterClause.IncludeWhiteWinningEnding = ((CheckBox)listBoxEnding.Items[0]).IsChecked!.Value;
                    m_filterClause.IncludeBlackWinningEnding = ((CheckBox)listBoxEnding.Items[1]).IsChecked!.Value;
                    m_filterClause.IncludeDrawEnding         = ((CheckBox)listBoxEnding.Items[2]).IsChecked!.Value;
                }
            }
            if (m_filterClause.IsAllRanges       &&
                m_filterClause.IncludeAllPlayers &&
                m_filterClause.IncludeAllEnding  &&
                m_filterClause.IncludesUnrated) {
                MessageBox.Show("At least one filtering option must be selected.");
                retVal = false;
            }
            return retVal;
        }

        /// <summary>
        /// Clause use to filter the PGN file has defined by the user. Valid after the Ok button has been clicked.
        /// </summary>
        public PgnUtil.FilterClause FilteringClause => m_filterClause;

        /// <summary>
        /// Called when the Ok button is clicked
        /// </summary>
        /// <param name="sender"> Sender object</param>
        /// <param name="e">      Event argument</param>
        private void ButOk_Click(object sender, RoutedEventArgs e) {
            if (SyncInfo()) {
                PgnUtil.CreateSubsetPgn(m_pgnParser!,
                                        m_pgnGames!,
                                        m_filterClause);
            }
        }

        /// <summary>
        /// Called when the Ok button is clicked
        /// </summary>
        /// <param name="sender"> Sender object</param>
        /// <param name="e">      Event argument</param>
        private void ButTest_Click(object sender, RoutedEventArgs e) {
            int count;
            
            if (SyncInfo()) {
                count = PgnUtil.FilterPgn(m_pgnParser!, m_pgnGames!, textWriter: null, FilteringClause);
                MessageBox.Show($"The specified filter will result in {count} game(s) selected.");
            }
        }

        /// <summary>
        /// Called when the button is clicked
        /// </summary>
        /// <param name="sender"> Sender object</param>
        /// <param name="e">      Event argument</param>
        private void ButSelectAllRange_Click(object sender, RoutedEventArgs e) => CheckAllItems(listBoxRange, isChecked: true);

        /// <summary>
        /// Called when the button is clicked
        /// </summary>
        /// <param name="sender"> Sender object</param>
        /// <param name="e">      Event argument</param>
        private void ButClearAllRange_Click(object sender, RoutedEventArgs e) => CheckAllItems(listBoxRange, isChecked: false);

        /// <summary>
        /// Called when the button is clicked
        /// </summary>
        /// <param name="sender"> Sender object</param>
        /// <param name="e">      Event argument</param>
        private void ButSelectAllPlayers_Click(object sender, RoutedEventArgs e) => CheckAllItems(listBoxPlayer, isChecked: true);

        /// <summary>
        /// Called when the button is clicked
        /// </summary>
        /// <param name="sender"> Sender object</param>
        /// <param name="e">      Event argument</param>
        private void ButClearAllPlayers_Click(object sender, RoutedEventArgs e) => CheckAllItems(listBoxPlayer, isChecked: false);

        /// <summary>
        /// Called when the button is clicked
        /// </summary>
        /// <param name="sender"> Sender object</param>
        /// <param name="e">      Event argument</param>
        private void ButSelectAllEndGame_Click(object sender, RoutedEventArgs e) => CheckAllItems(listBoxEnding, isChecked: true);

        /// <summary>
        /// Called when the button is clicked
        /// </summary>
        /// <param name="sender"> Sender object</param>
        /// <param name="e">      Event argument</param>
        private void ButClearAllEndGame_Click(object sender, RoutedEventArgs e) => CheckAllItems(listBoxEnding, isChecked: false);

        /// <summary>
        /// Called when the button is clicked
        /// </summary>
        private void CheckBoxAllRanges_CheckedChanged() {
            listBoxRange.IsEnabled      = !checkBoxAllRanges.IsChecked!.Value;
            butClearAllRange.IsEnabled  = !checkBoxAllRanges.IsChecked!.Value;
            butSelectAllRange.IsEnabled = !checkBoxAllRanges.IsChecked!.Value;
        }

        /// <summary>
        /// Called when the button is clicked
        /// </summary>
        private void CheckBoxAllPlayer_CheckedChanged() {
            listBoxPlayer.IsEnabled         = !(bool)checkBoxAllPlayer.IsChecked!;
            butClearAllPlayers.IsEnabled    = !(bool)checkBoxAllPlayer.IsChecked!;
            butSelectAllPlayers.IsEnabled   = !(bool)checkBoxAllPlayer.IsChecked!;
        }

        /// <summary>
        /// Called when the button is clicked
        /// </summary>
        private void CheckBoxAllEndGame_CheckedChanged() {
            listBoxEnding.IsEnabled         = !(bool)checkBoxAllEndGame.IsChecked!;
            butClearAllEndGame.IsEnabled    = !(bool)checkBoxAllEndGame.IsChecked!;
            butSelectAllEndGame.IsEnabled   = !(bool)checkBoxAllEndGame.IsChecked!;
        }

        /// <summary>
        /// Called when the All Range checkbox is checked
        /// </summary>
        /// <param name="sender"> sender object</param>
        /// <param name="e">      event argument</param>
        private void CheckBoxAllRanges_Checked(object sender, RoutedEventArgs e) => CheckBoxAllRanges_CheckedChanged();

        /// <summary>
        /// Called when the All Ranges checkbox is unchecked
        /// </summary>
        /// <param name="sender"> sender object</param>
        /// <param name="e">      event argument</param>
        private void CheckBoxAllRanges_Unchecked(object sender, RoutedEventArgs e) => CheckBoxAllRanges_CheckedChanged();

        /// <summary>
        /// Called when the All Players checkbox is checked
        /// </summary>
        /// <param name="sender"> sender object</param>
        /// <param name="e">      event argument</param>
        private void CheckBoxAllPlayer_Checked(object sender, RoutedEventArgs e) => CheckBoxAllPlayer_CheckedChanged();

        /// <summary>
        /// Called when the All Players checkbox is unchecked
        /// </summary>
        /// <param name="sender"> sender object</param>
        /// <param name="e">      event argument</param>
        private void CheckBoxAllPlayer_Unchecked(object sender, RoutedEventArgs e) => CheckBoxAllPlayer_CheckedChanged();

        /// <summary>
        /// Called when the All End Games checkbox is checked
        /// </summary>
        /// <param name="sender"> sender object</param>
        /// <param name="e">      event argument</param>
        private void CheckBoxAllEndGame_Checked(object sender, RoutedEventArgs e) => CheckBoxAllEndGame_CheckedChanged();

        /// <summary>
        /// Called when the All End Games checkbox is unchecked
        /// </summary>
        /// <param name="sender"> sender object</param>
        /// <param name="e">      event argument</param>
        private void CheckBoxAllEndGame_Unchecked(object sender, RoutedEventArgs e) => CheckBoxAllEndGame_CheckedChanged();

    } // Class FrmPgnFilter
} // Namespace
