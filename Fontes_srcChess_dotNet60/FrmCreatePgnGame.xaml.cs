using System;
using System.Collections.Generic;
using System.Windows;
using SrcChess2.Core;
using SrcChess2.PgnParsing;

namespace SrcChess2 {
    /// <summary>
    /// Interaction logic for FrmCreatePgnGame.xaml
    /// </summary>
    public partial class FrmCreatePgnGame : Window {
        /// <summary>Array of move list</summary>
        public List<MoveExt>?         MoveList { get; private set; }
        /// <summary>Board starting position</summary>
        public ChessBoard?            StartingChessBoard { get; private set; }
        /// <summary>Starting Color</summary>
        public ChessBoard.PlayerColor StartingColor { get; private set; }
        /// <summary>Name of the player playing white</summary>
        public string?                WhitePlayerName { get; private set; }
        /// <summary>Name of the player playing black</summary>
        public string?                BlackPlayerName { get; private set; }
        /// <summary>Player type (computer or human)</summary>
        public PgnPlayerType          WhitePlayerType { get; private set; }
        /// <summary>Player type (computer or human)</summary>
        public PgnPlayerType          BlackPlayerType { get; private set; }
        /// <summary>White player playing time</summary>
        public TimeSpan               WhiteTimer { get; private set; }
        /// <summary>Black player playing time</summary>
        public TimeSpan               BlackTimer { get; private set; }

        /// <summary>
        /// Class Ctor
        /// </summary>
        public FrmCreatePgnGame() {
            InitializeComponent();
            StartingColor = ChessBoard.PlayerColor.White;
        }

        /// <summary>
        /// Accept the content of the form
        /// </summary>
        /// <param name="sender"> Sender object</param>
        /// <param name="e">      Event argument</param>
        private void ButOk_Click(object sender, RoutedEventArgs e) {
            string    game;
            PgnParser parser;

            game = textBox1.Text;
            if (string.IsNullOrEmpty(game)) {
                MessageBox.Show("No PGN text has been pasted.");
            } else {
                parser      = new PgnParser(false);
                parser.InitFromString(game);
                if (!parser.ParseSingle(ignoreMoveListIfFen: false,
                                        out int skipped,
                                        out int truncated,
                                        out PgnGame? pgnGame,
                                        out string? errTxt)) {
                    MessageBox.Show($"The specified board is invalid - {(errTxt ?? "")}");
                } else if (skipped != 0) {
                    MessageBox.Show("The game is incomplete. Paste another game.");
                } else if (truncated != 0) {
                    MessageBox.Show("The selected game includes an unsupported pawn promotion (only pawn promotion to queen is supported).");
                } else if (pgnGame!.MoveExtList!.Count == 0 && pgnGame.StartingChessBoard == null) {
                    MessageBox.Show("Game is empty.");
                } else {
                    MoveList           = pgnGame.MoveExtList;
                    StartingChessBoard = pgnGame.StartingChessBoard;
                    StartingColor      = pgnGame.StartingColor;
                    WhitePlayerName    = pgnGame.WhitePlayerName;
                    BlackPlayerName    = pgnGame.BlackPlayerName;
                    WhitePlayerType    = pgnGame.WhiteType;
                    BlackPlayerType    = pgnGame.BlackType;
                    WhiteTimer         = pgnGame.WhiteSpan;
                    BlackTimer         = pgnGame.BlackSpan;
                    DialogResult       = true;
                    Close();
                }
            }
        }
    } // Class FrmCreatePgnGame
} // Namespace
