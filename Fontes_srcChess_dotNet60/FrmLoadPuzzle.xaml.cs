using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SrcChess2.Core;
using SrcChess2.PgnParsing;

namespace SrcChess2 {
    /// <summary>
    /// Interaction logic for frmLoadPuzzle.xaml
    /// </summary>
    public partial class FrmLoadPuzzle : Window {

        /// <summary>
        /// Puzzle item class use to fill the listview
        /// </summary>
        public class PuzzleItem {
            /// <summary>Puzzle id</summary>
            public  int    Id { get; private set; }
            /// <summary>Puzzle description</summary>
            public  string Description { get; private set; }
            /// <summary>true if this puzzle has been done</summary>
            public  bool   Done { get; set; }

            /// <summary>
            /// Ctor
            /// </summary>
            /// <param name="id">           Puzzle id</param>
            /// <param name="description">  Description</param>
            /// <param name="isDone">       true if already been done</param>
            public PuzzleItem(int id, string description, bool isDone) {
                Id          = id;
                Description = description;
                Done        = isDone;
            }

        }

        /// <summary>List of PGN Games</summary>
        static private List<PgnGame>? m_pgnGameList;
        /// <summary>PGN parser</summary>
        private readonly PgnParser    m_pgnParser;
        /// <summary>Done mask</summary>
        private readonly long[]?      m_doneMask;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="doneMask">   Mask of game which has been done</param>
        public FrmLoadPuzzle(long[]? doneMask) {
            List<PuzzleItem> puzzleItemList;
            PuzzleItem       puzzleItem;
            int              count;
            bool             hasBeenDone;

            InitializeComponent();
            m_doneMask  = doneMask;
            m_pgnParser = new PgnParser(false);
            if (m_pgnGameList == null) {
                BuildPuzzleList();
            }
            puzzleItemList = new List<PuzzleItem>(m_pgnGameList!.Count);
            count          = 0;
            foreach (PgnGame pgnGame in m_pgnGameList) {
                if (doneMask == null) {
                    hasBeenDone = false;
                } else {
                    hasBeenDone = (doneMask[count / 64] & (1L << (count & 63))) != 0;
                }
                count++;
                puzzleItem  = new(count, pgnGame.Event ?? "", hasBeenDone);
                puzzleItemList.Add(puzzleItem);
            }
            listViewPuzzle.ItemsSource   = puzzleItemList;
            listViewPuzzle.SelectedIndex = 0;
        }

        /// <summary>
        /// Ctor
        /// </summary>
        public FrmLoadPuzzle() : this(null) {}

        /// <summary>
        /// Load PGN text from resource
        /// </summary>
        /// <returns>PGN text</returns>
        private string LoadPgn() {
            string                 retVal;
            Assembly               assem;
            System.IO.Stream?      stream;
            System.IO.StreamReader reader;

            assem   = GetType().Assembly;
            stream  = assem.GetManifestResourceStream("SrcChess2.111probs.pgn");
            reader  = new System.IO.StreamReader(stream ?? throw new InvalidOperationException("Unable to find the SrcChess2.111probs.pgn resource"),
                                                 Encoding.ASCII);
            try {
                retVal = reader.ReadToEnd();
            } finally {
                reader.Dispose();
            }
            return retVal;
        }

        /// <summary>
        /// Build a list of puzzles using the PGN find in resource
        /// </summary>
        private void BuildPuzzleList() {
            string  pgn;

            pgn           = LoadPgn();
            m_pgnParser.InitFromString(pgn);
            m_pgnGameList = m_pgnParser.GetAllRawPgn(getAttrList: true, getMoveList: false, out int _);
        }

        /// <summary>
        /// Gets the selected game
        /// </summary>
        public PgnGame Game {
            get {
                PgnGame retVal;

                retVal                    = m_pgnGameList![listViewPuzzle.SelectedIndex];
                m_pgnParser.ParseFen(retVal.Fen ?? "", out ChessBoard.PlayerColor playerColor, out ChessBoard? board);
                retVal.StartingColor      = playerColor;
                retVal.StartingChessBoard = board;
                return retVal;
            }
        }

        /// <summary>
        /// Returns the selected game index
        /// </summary>
        public int GameIndex => listViewPuzzle.SelectedIndex;

        /// <summary>
        /// Called when the OK button is pressed
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event arguments</param>
        private void ButOk_Click(object sender, RoutedEventArgs e) => DialogResult = true;

        /// <summary>
        /// Called when the Cancel button is pressed
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event arguments</param>
        private void ButCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        /// <summary>
        /// Called when the Reset Done button is pressed
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event arguments</param>
        private void ButResetDone_Click(object sender, RoutedEventArgs e) {
            List<PuzzleItem> puzzleItemList;

            if (MessageBox.Show("Are you sure you want to reset the Done state of all puzzles to false?", "", MessageBoxButton.YesNo) == MessageBoxResult.Yes) {
                for (int i = 0; i < m_doneMask!.Length; i++) {
                    m_doneMask[i] = 0;
                }
                puzzleItemList = (List<PuzzleItem>)listViewPuzzle.ItemsSource;
                foreach (PuzzleItem item in puzzleItemList) {
                    item.Done = false;
                }
                listViewPuzzle.ItemsSource = null;
                listViewPuzzle.ItemsSource = puzzleItemList;
            }
        }

        /// <summary>
        /// Called when a selection is changed
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event arguments</param>
        private void ListViewPuzzle_SelectionChanged(object sender, SelectionChangedEventArgs e) => butOk.IsEnabled = listViewPuzzle.SelectedIndex != -1;

        /// <summary>
        /// Called when a selection is double clicked
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event arguments</param>
        private void ListViewPuzzle_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
            if (listViewPuzzle.SelectedIndex != -1) {
                DialogResult = true;
            }
        }
    } // Class frmLoadPuzzle
} // Namespace
