using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SrcChess2 {
    /// <summary>
    /// Interaction logic for CustomColorPicker.xaml
    /// </summary>
    public partial class CustomColorPicker : UserControl{
        private Color   m_selectedColor      = Colors.Transparent;
        private bool    m_isContexMenuOpened = false;

        /// <summary>
        /// Class Ctor
        /// </summary>
        public CustomColorPicker() {
            InitializeComponent();
            b.ContextMenu.Closed       += new RoutedEventHandler(ContextMenu_Closed);
            b.ContextMenu.Opened       += new RoutedEventHandler(ContextMenu_Opened);
            b.PreviewMouseLeftButtonUp += new MouseButtonEventHandler(B_PreviewMouseLeftButtonUp);
        }

        
        /// <summary>
        /// SelectedColor event
        /// </summary>
        public event Action<Color>? SelectedColorChanged;

        /// <summary>
        /// Color in Hexadecimal
        /// </summary>
        public string HexValue { get; set; } = "";

        /// <summary>
        /// Selected Color
        /// </summary>
        public Color SelectedColor{
            get => m_selectedColor;
            set {
                if (m_selectedColor != value) {
                    m_selectedColor = value;
                    cp.CustomColor  = value;
                    Update();
                }
            }
        }

        /// <summary>
        /// Called when the context menu is opened
        /// </summary>
        /// <param name="sender"> Sender object</param>
        /// <param name="e">      Event arguments</param>
        private void ContextMenu_Opened(object sender, RoutedEventArgs e) => m_isContexMenuOpened = true;

        /// <summary>
        /// Update the color
        /// </summary>
        private void Update() {
            recContent.Fill = new SolidColorBrush(cp.CustomColor);
            HexValue        = string.Format("#{0}", cp.CustomColor.ToString()[1..]);
            m_selectedColor = cp.CustomColor;
        }

        /// <summary>
        /// Called when the context menu is closed
        /// </summary>
        /// <param name="sender"> Sender object</param>
        /// <param name="e">      Event arguments</param>
        private void ContextMenu_Closed(object sender, RoutedEventArgs e) {
            if (!b.ContextMenu.IsOpen) {
                SelectedColorChanged?.Invoke(cp.CustomColor);
                Update();
            }
            m_isContexMenuOpened = false;
        }


        /// <summary>
        /// Called when the mouse left button is up
        /// </summary>
        /// <param name="sender"> Sender object</param>
        /// <param name="e">      Event arguments</param>
        private void B_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            if (!m_isContexMenuOpened) {
                if (b.ContextMenu != null && b.ContextMenu.IsOpen == false) {
                    b.ContextMenu.PlacementTarget   = b;
                    b.ContextMenu.Placement         = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                    ContextMenuService.SetPlacement(b, System.Windows.Controls.Primitives.PlacementMode.Bottom);
                    b.ContextMenu.IsOpen            = true;
                }
            }
        }
    }
}
