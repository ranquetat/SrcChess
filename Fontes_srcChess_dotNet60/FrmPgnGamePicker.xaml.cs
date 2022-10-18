using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Text;
using SrcChess2.Core;
using SrcChess2.PgnParsing;

namespace SrcChess2 {
    /// <summary>
    /// Interaction logic for FrmPgnGamePicker.xaml
    /// </summary>
    public partial class FrmPgnGamePicker : Window {
        /// <summary>Item used to fill the description listbox so we can find the original index in the list after a sort</summary>
        private class PgnGameDescItem : IComparable<PgnGameDescItem> {
            /// <summary>Description of the item</summary>
            public string Description { get; private set; }

            /// <summary>Index of the item</summary>
            public int Index { get; private set; }

            /// <summary>
            /// Class constructor
            /// </summary>
            /// <param name="desc">  Item description</param>
            /// <param name="index"> Item index</param>
            public PgnGameDescItem(string desc, int index) {
                Description = desc;
                Index       = index;
            }

            /// <summary>
            /// IComparable interface
            /// </summary>
            /// <param name="other"> Item to compare with</param>
            /// <returns>
            /// -1, 0, 1
            /// </returns>
            public int CompareTo(PgnGameDescItem? other) => string.Compare(Description, other?.Description);

            /// <summary>
            /// Return the description
            /// </summary>
            /// <returns>
            /// Description
            /// </returns>
            public override string ToString() => Description;

        } // Class PgnGameDescItem

        /// <summary>List of moves for the current game</summary>
        public List<MoveExt>?         MoveList { get; private set; }
        /// <summary>Selected game</summary>
        public string?                SelectedGame { get; private set; }
        /// <summary>Starting board. Null if standard board</summary>
        public ChessBoard?            StartingChessBoard { get; private set; }
        /// <summary>Starting color</summary>
        public ChessBoard.PlayerColor StartingColor { get; private set; }
        /// <summary>White Player Name</summary>
        public string                 WhitePlayerName { get; private set; } = "";
        /// <summary>Black Player Name</summary>
        public string                 BlackPlayerName { get; private set; } = "";
        /// <summary>White Player Type</summary>
        public PgnPlayerType          WhitePlayerType { get; private set; }
        /// <summary>Black Player Type</summary>
        public PgnPlayerType          BlackPlayerType { get; private set; }
        /// <summary>White Timer</summary>
        public TimeSpan               WhiteTimer { get; private set; }
        /// <summary>Black Timer</summary>
        public TimeSpan               BlackTimer { get; private set; }
        /// <summary>List of games</summary>
        private List<PgnGame>         m_pgnGames;
        /// <summary>PGN parser</summary>
        private readonly PgnParser    m_pgnParser;

        /// <summary>
        /// Class Ctor
        /// </summary>
        public FrmPgnGamePicker() {
            InitializeComponent();
            m_pgnParser        = new PgnParser(false /*bDiagnose*/);
            SelectedGame       = null;
            StartingColor      = ChessBoard.PlayerColor.White;
            StartingChessBoard = null;
            m_pgnGames         = new List<PgnGame>(65536);
        }

        /// <summary>
        /// Get the selected game content
        /// </summary>
        /// <returns>
        /// Game or null if none selected
        /// </returns>
        private string? GetSelectedGame() {
            string? retVal;
            PgnGame pgnGame;
            int     selIndex;
            
            selIndex = listBoxGames.SelectedIndex;
            if (selIndex != -1) {
                pgnGame = m_pgnGames[selIndex];
                retVal  = m_pgnParser.PgnLexical!.GetStringAtPos(pgnGame.StartingPos, pgnGame.Length);
            } else {
                retVal  = null;
            }
            return retVal;
        }

        /// <summary>
        /// Refresh the textbox containing the selected game content
        /// </summary>
        private void RefreshGameDisplay() {
            SelectedGame        = GetSelectedGame();
            textBoxGame.Text    = SelectedGame ?? "";
        }

        /// <summary>
        /// Get game description
        /// </summary>
        /// <param name="pgnGame">  PGN game</param>
        /// <returns></returns>
        protected virtual string GetGameDesc(PgnGame pgnGame) {
            StringBuilder   strb;

            strb = new StringBuilder(128);
            strb.Append(pgnGame.WhitePlayerName ?? "???");
            strb.Append(" against ");
            strb.Append(pgnGame.BlackPlayerName ?? "???");
            strb.Append(" (");
            strb.Append((pgnGame.WhiteElo  == -1) ? "-" : pgnGame.WhiteElo.ToString());
            strb.Append('/');
            strb.Append((pgnGame.BlackElo  == -1) ? "-" : pgnGame.BlackElo.ToString());
            strb.Append(") played on ");
            strb.Append(pgnGame.Date ?? "???");
            strb.Append(". Result is ");
            strb.Append(pgnGame.GameResult ?? "???");
            return strb.ToString();
        }

        /// <summary>
        /// Initialize the form with the content of the PGN file
        /// </summary>
        /// <param name="fileName"> PGN file name</param>
        /// <returns>
        /// true if at least one game has been found.
        /// </returns>
        public bool InitForm(string fileName) {
            bool    retVal;
            int     index;
            string  desc;

            retVal = m_pgnParser.InitFromFile(fileName);
            if (retVal) {
                try {
                    m_pgnGames = m_pgnParser.GetAllRawPgn(getAttrList: true, getMoveList: false, out int _);
                    if (m_pgnGames.Count < 1) {
                        MessageBox.Show($"No games found in the PGN File '{fileName}'");
                        retVal = false;
                    } else {
                        index  = 0;
                        foreach (PgnGame pgnGame in m_pgnGames) {
                            desc = (index + 1).ToString().PadLeft(5, '0') + " - " + GetGameDesc(pgnGame);
                            listBoxGames.Items.Add(new PgnGameDescItem(desc, index));
                            index++;
                        }
                        listBoxGames.SelectedIndex = 0;
                        retVal                     = true;
                    }
                } catch (PgnParserException ex) {
                    MessageBox.Show($"Error parsing the PGN File '{fileName}' - {ex.Message}\r\n {ex.CodeInError}");
                    retVal = false;
                }
            }
            return retVal;
        }

        /// <summary>
        /// Called when a game is selected
        /// </summary>
        /// <param name="noMoveList">   true to ignore the move list</param>
        private void GameSelected(bool noMoveList) {
            string?     game;
            PgnParser   parser;

            game = GetSelectedGame();
            if (game != null) {
                parser      = new PgnParser(false);
                parser.InitFromString(game);
                if (!parser.ParseSingle(noMoveList,
                                        out int skip,
                                        out int truncated,
                                        out PgnGame? pgnGame,
                                        out string?  errTxt)) {
                    MessageBox.Show($"The specified board is invalid - {errTxt ?? ""}");
                } else if (skip != 0) {
                    MessageBox.Show("The game is incomplete. Select another game.");
                } else if (truncated != 0) {
                    MessageBox.Show("The selected game includes an unsupported pawn promotion (only pawn promotion to queen is supported).");
                } else if (pgnGame!.MoveExtList!.Count == 0 && pgnGame.StartingChessBoard == null) {
                    MessageBox.Show("Game is empty.");
                } else {
                    StartingChessBoard  = pgnGame.StartingChessBoard;
                    StartingColor       = pgnGame.StartingColor;
                    WhitePlayerName     = pgnGame.WhitePlayerName ?? "";
                    BlackPlayerName     = pgnGame.BlackPlayerName ?? "";
                    WhitePlayerType     = pgnGame.WhiteType;
                    BlackPlayerType     = pgnGame.BlackType;
                    WhiteTimer          = pgnGame.WhiteSpan;
                    BlackTimer          = pgnGame.BlackSpan;
                    MoveList            = pgnGame.MoveExtList;
                    DialogResult        = true;
                    Close();
                }
            }
        }

        /// <summary>
        /// Accept the content of the form
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event argument</param>
        private void Button_Click(object sender, RoutedEventArgs e) => GameSelected(noMoveList: false);

        /// <summary>
        /// Accept the content of the form (but no move)
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event argument</param>
        private void Button_Click_1(object sender, RoutedEventArgs e) => GameSelected(noMoveList: true);

        /// <summary>
        /// Called when the game selection is changed
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event argument</param>
        private void ListBoxGames_SelectionChanged(object sender, SelectionChangedEventArgs e) => RefreshGameDisplay();

    } // Class frmPgnGamePicker
} // Namespace
