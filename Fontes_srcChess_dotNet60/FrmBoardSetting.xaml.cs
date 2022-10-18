using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SrcChess2 {
    /// <summary>Pickup the colors use to draw the chess control</summary>
    public partial class FrmBoardSetting : Window {
        /// <summary>Lite Cell Color</summary>
        public Color                                 LiteCellColor { get; private set; }
        /// <summary>Dark Cell Color</summary>
        public Color                                 DarkCellColor { get; private set; }
        /// <summary>White Piece Color</summary>
        public Color                                 WhitePieceColor { get; private set; }
        /// <summary>Black Piece Color</summary>
        public Color                                 BlackPieceColor { get; private set; }
        /// <summary>Background Color</summary>
        public Color                                 BackgroundColor { get; private set; }
        /// <summary>Selected PieceSet</summary>
        public PieceSet?                             PieceSet { get; private set; }
        /// <summary>List of Piece Sets</summary>
        private readonly SortedList<string,PieceSet> m_pieceSetList = new(0);

        /// <summary>
        /// Class Ctor
        /// </summary>
        public FrmBoardSetting() => InitializeComponent();

        /// <summary>
        /// Class constructor
        /// </summary>
        /// <param name="liteCellColor">   Lite Cells Color</param>
        /// <param name="darkCellColor">   Dark Cells Color</param>
        /// <param name="whitePieceColor"> White Pieces Color</param>
        /// <param name="blackPieceColor"> Black Pieces Color</param>
        /// <param name="backGroundColor"> Main window background color</param>
        /// <param name="pieceSetList">    List of Piece Sets</param>
        /// <param name="pieceSet">        Current Piece Set</param>
        public FrmBoardSetting(Color liteCellColor, Color darkCellColor, Color whitePieceColor, Color blackPieceColor, Color backGroundColor, SortedList<string, PieceSet> pieceSetList, PieceSet pieceSet) {
            InitializeComponent();
            LiteCellColor              = liteCellColor;
            DarkCellColor              = darkCellColor;
            WhitePieceColor            = whitePieceColor;
            BlackPieceColor            = blackPieceColor;
            BackgroundColor            = backGroundColor;
            m_pieceSetList             = pieceSetList;
            PieceSet                   = pieceSet;
            m_chessCtl.LiteCellColor   = liteCellColor;
            m_chessCtl.DarkCellColor   = darkCellColor;
            m_chessCtl.WhitePieceColor = whitePieceColor;
            m_chessCtl.BlackPieceColor = blackPieceColor;
            m_chessCtl.PieceSet        = pieceSet;
            Background                 = new SolidColorBrush(BackgroundColor);
            Loaded                    += new RoutedEventHandler(FrmBoardSetting_Loaded);
            FillPieceSet();
        }

        /// <summary>
        /// Called when the form is loaded
        /// </summary>
        /// <param name="sender"> Sender Object</param>
        /// <param name="e">      Event parameter</param>
        private void FrmBoardSetting_Loaded(object sender, RoutedEventArgs e) {
            customColorPickerLite.SelectedColor         = LiteCellColor;
            customColorPickerDark.SelectedColor         = DarkCellColor;
            customColorBackground.SelectedColor         = BackgroundColor;
            customColorPickerLite.SelectedColorChanged += new Action<Color>(CustomColorPickerLite_SelectedColorChanged);
            customColorPickerDark.SelectedColorChanged += new Action<Color>(CustomColorPickerDark_SelectedColorChanged);
            customColorBackground.SelectedColorChanged += new Action<Color>(CustomColorBackground_SelectedColorChanged);
        }

        /// <summary>
        /// Called when the dark cell color is changed
        /// </summary>
        /// <param name="color">    Color</param>
        private void CustomColorPickerDark_SelectedColorChanged(Color color) {
            DarkCellColor            = color;
            m_chessCtl.DarkCellColor = DarkCellColor;
        }

        /// <summary>
        /// Called when the lite cell color is changed
        /// </summary>
        /// <param name="color"> Color</param>
        private void CustomColorPickerLite_SelectedColorChanged(Color color) {
            LiteCellColor            = color;
            m_chessCtl.LiteCellColor = LiteCellColor;
        }

        /// <summary>
        /// Called when the background color is changed
        /// </summary>
        /// <param name="color"> Color</param>
        private void CustomColorBackground_SelectedColorChanged(Color color) {
            BackgroundColor = color;
            Background      = new SolidColorBrush(BackgroundColor);
        }


        /// <summary>
        /// Fill the combo box with the list of piece sets
        /// </summary>
        private void FillPieceSet() {
            int index;

            comboBoxPieceSet.Items.Clear();
            foreach (PieceSet pieceSet in m_pieceSetList.Values) {
                index = comboBoxPieceSet.Items.Add(pieceSet.Name);
                if (pieceSet == PieceSet) {
                    comboBoxPieceSet.SelectedIndex = index;
                }
            }
        }

        /// <summary>
        /// Called when the reset to default button is pressed
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event handler</param>
        private void ButResetToDefault_Click(object sender, RoutedEventArgs e) {
            LiteCellColor                       = Colors.Moccasin;
            DarkCellColor                       = Colors.SaddleBrown;
            BackgroundColor                     = Colors.SkyBlue;
            PieceSet                            = m_pieceSetList["leipzig"];
            Background                          = new SolidColorBrush(BackgroundColor);
            m_chessCtl.LiteCellColor            = LiteCellColor;
            m_chessCtl.DarkCellColor            = DarkCellColor;
            m_chessCtl.PieceSet                 = PieceSet;
            customColorPickerLite.SelectedColor = LiteCellColor;
            customColorPickerDark.SelectedColor = DarkCellColor;
            customColorBackground.SelectedColor = BackgroundColor;
            comboBoxPieceSet.SelectedItem       = PieceSet.Name;
        }

        /// <summary>
        /// Called when the PieceSet is changed
        /// </summary>
        /// <param name="sender"> Sender Object</param>
        /// <param name="e">      Event argument</param>
        private void ComboBoxPieceSet_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            int     selectedIndex;
            string  val;

            selectedIndex  = comboBoxPieceSet.SelectedIndex;
            if (selectedIndex != -1) {
                val                 = (string)comboBoxPieceSet.Items[selectedIndex];
                PieceSet            = m_pieceSetList[val];
                m_chessCtl.PieceSet = PieceSet;
            }
        }

        /// <summary>
        /// Called when the Ok button is clicked
        /// </summary>
        /// <param name="sender"> Sender Object</param>
        /// <param name="e">      Event argument</param>
        private void ButOk_Click(object sender, RoutedEventArgs e) {
            DialogResult = true;
            Close();
        }
    } // Class FrmBoardSetting
} // Namespace
