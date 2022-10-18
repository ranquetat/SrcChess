using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Reflection;

namespace SrcChess2 {   

    /// <summary>
    /// Color Picker
    /// </summary>
    public partial class ColorPicker : UserControl {
        /// <summary>true if mouse button is down over the elipse</summary>
        private bool    m_isMouseDownOverEllipse = false;
        /// <summary>true if shift is down</summary>
        private bool    m_shift = false;
        /// <summary>Custom color</summary>
        private Color   m_customColor = Colors.Transparent;
       
        /// <summary>
        /// Class Ctor
        /// </summary>
        public ColorPicker() {
            InitializeComponent();
            InitialWork();
            image.Source                   = LoadBitmap(Properties.Resources.ColorSwatchCircle);
            txtAlpha.LostFocus            += new RoutedEventHandler(Txt_TextChanged);
            txtR.LostFocus                += new RoutedEventHandler(Txt_TextChanged);
            txtG.LostFocus                += new RoutedEventHandler(Txt_TextChanged);
            txtB.LostFocus                += new RoutedEventHandler(Txt_TextChanged);
            txtAll.LostFocus              += new RoutedEventHandler(TxtAll_TextChanged);
            txtAlpha.KeyDown              += new KeyEventHandler(TxtAlpha_KeyDown);
            txtR.KeyDown                  += new KeyEventHandler(TxtR_KeyDown);
            txtG.KeyDown                  += new KeyEventHandler(TxtG_KeyDown);
            txtB.KeyDown                  += new KeyEventHandler(TxtB_KeyDown);
            txtAll.KeyDown                += new KeyEventHandler(TxtAll_KeyDown);
            CanColor.MouseLeftButtonDown  += new MouseButtonEventHandler(CanColor_MouseLeftButtonDown);
            CanColor.MouseLeftButtonUp    += new MouseButtonEventHandler(CanColor_MouseLeftButtonUp);
            EpPointer.MouseMove           += new MouseEventHandler(EpPointer_MouseMove);
            EpPointer.MouseLeftButtonDown += new MouseButtonEventHandler(EpPointer_MouseLeftButtonDown);
            EpPointer.MouseLeftButtonUp   += new MouseButtonEventHandler(EpPointer_MouseLeftButtonUp);
        }
        
        /// <summary>
        /// Class Ctor
        /// </summary>
        public Color CustomColor {
            get => m_customColor;
            set {
                if (m_customColor != value) {
                    m_customColor = value;
                    UpdatePreview();
                }
            }
        }

        /// <summary>
        /// Called when the left mouse button is released
        /// </summary>
        /// <param name="sender"> Sender object</param>
        /// <param name="e">      Event arguments</param>
        private void EpPointer_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) => m_isMouseDownOverEllipse = false;

        /// <summary>
        /// Called when the left mouse button is released
        /// </summary>
        /// <param name="sender"> Sender object</param>
        /// <param name="e">      Event arguments</param>
        private void CanColor_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) => e.Handled = true;

        /// <summary>
        /// Called when the left mouse button is pressed
        /// </summary>
        /// <param name="sender"> Sender object</param>
        /// <param name="e">      Event arguments</param>
        private void CanColor_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            ChangeColor();
            e.Handled = true;
        }

        /// <summary>
        /// Called when the left mouse button is pressed
        /// </summary>
        /// <param name="sender"> Sender object</param>
        /// <param name="e">      Event arguments</param>
        private void EpPointer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => m_isMouseDownOverEllipse = true;

        /// <summary>
        /// Called when the left mouse is moved
        /// </summary>
        /// <param name="sender"> Sender object</param>
        /// <param name="e">      Event arguments</param>
        private void EpPointer_MouseMove(object sender, MouseEventArgs e) {
            if (m_isMouseDownOverEllipse) {
                ChangeColor();
            }
            e.Handled = true;
        }

        /// <summary>
        /// Called when a key is down
        /// </summary>
        /// <param name="sender"> Sender object</param>
        /// <param name="e">      Event arguments</param>
        public void TxtAll_KeyDown(object sender, KeyEventArgs e) {           

            if (e.Key == Key.Enter) {
                try {
                    if (string.IsNullOrEmpty(((TextBox)sender).Text)) return;
                    CustomColor = MakeColorFromHex(sender);
                    Reposition();
                }
                catch {}
            } else if (e.Key == Key.Tab) {
                txtAlpha.Focus();
            }
            
            string input = e.Key.ToString()[1..];
            if (string.IsNullOrEmpty(input)) {
                input = e.Key.ToString();
            }
            if (input == "3" && m_shift == true) {
                input = "#";
            }
            if (e.Key == Key.LeftShift || e.Key == Key.RightShift){
                m_shift = true;
            } else {
                m_shift = false;
            }

            if (!(input == "#" || (input[0] >= 'A' && input[0] <= 'F') || (input[0] >= 'a' && input[0] <= 'F') || (input[0] >= '0' && input[0] <= '9'))) {
                e.Handled = true;
            }
            if (input.Length > 1) {
                e.Handled = true;
            }
        }

        /// <summary>
        /// Called when a key is down
        /// </summary>
        /// <param name="sender"> Sender object</param>
        /// <param name="e">      Event arguments</param>
        private void TxtB_KeyDown(object sender, KeyEventArgs e) {
            NumericValidation(e);
            if (e.Key == Key.Tab) {
                txtAll.Focus();
            }
        }

        /// <summary>
        /// Called when a key is down
        /// </summary>
        /// <param name="sender"> Sender object</param>
        /// <param name="e">      Event arguments</param>
        private void TxtG_KeyDown(object sender, KeyEventArgs e) {
            NumericValidation(e);
            if (e.Key == Key.Tab) {
                txtB.Focus();
            }
        }

        /// <summary>
        /// Called when a key is pressed
        /// </summary>
        /// <param name="sender"> Sender object</param>
        /// <param name="e">      Event arguments</param>
        private void TxtR_KeyDown(object sender, KeyEventArgs e) {
            NumericValidation(e);
            if (e.Key == Key.Tab) {
                txtG.Focus();
            }
        }

        /// <summary>
        /// Called when a key is pressed
        /// </summary>
        /// <param name="sender"> Sender object</param>
        /// <param name="e">      Event arguments</param>
        private void TxtAlpha_KeyDown(object sender, KeyEventArgs e) {
            NumericValidation(e);
            if (e.Key == Key.Tab) {
                txtR.Focus();
            }
        }

        /// <summary>
        /// Called when a key is pressed
        /// </summary>
        /// <param name="sender"> Sender object</param>
        /// <param name="e">      Event arguments</param>
        private void TxtAll_TextChanged(object sender, RoutedEventArgs e) {
            try {
                if (string.IsNullOrEmpty(((TextBox)sender).Text)) return;
                CustomColor = MakeColorFromHex(sender);
                Reposition();
            } catch {}
        }

        /// <summary>
        /// Gets the color from the sender object
        /// </summary>
        /// <param name="sender"> Sender object</param>
        private Color MakeColorFromHex(object sender) {
            try {
                ColorConverter cc = new();
                return (Color)(cc.ConvertFrom(((TextBox)sender).Text) ?? null!);
            } catch {
                string alphaHex = CustomColor.A.ToString("X").PadLeft(2, '0');
                string redHex   = CustomColor.R.ToString("X").PadLeft(2, '0');
                string greenHex = CustomColor.G.ToString("X").PadLeft(2, '0');
                string blueHex  = CustomColor.B.ToString("X").PadLeft(2, '0');
                txtAll.Text     = string.Format("#{0}{1}{2}{3}", alphaHex, redHex, greenHex, blueHex);
            }
            return m_customColor;
        }

        /// <summary>
        /// Called when the text has changed
        /// </summary>
        /// <param name="sender"> Sender object</param>
        /// <param name="e">      Event arguments</param>
        private void Txt_TextChanged(object sender, RoutedEventArgs e) {
            if (!string.IsNullOrEmpty(((TextBox)sender).Text)) {
                if (int.TryParse(((TextBox)sender).Text, out int val)) {
                    if (val > 255) {
                        ((TextBox)sender).Text = "255";
                    } else {
                        if (MakeColorFromRGB(out Color color)) {
                            CustomColor = color;
                            Reposition();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the color from the text control
        /// </summary>
        /// <param name="color"> Returned color</param>
        private bool MakeColorFromRGB(out Color color) {
            bool retVal;
            byte rbyteValue = 0;
            byte gbyteValue = 0;
            byte bbyteValue = 0;
            
            retVal =   Byte.TryParse(txtAlpha.Text, out byte abyteValue)    &&
                       Byte.TryParse(txtR.Text, out rbyteValue)             &&
                       Byte.TryParse(txtG.Text, out gbyteValue)             &&
                       Byte.TryParse(txtB.Text, out bbyteValue);
            color = Color.FromArgb(abyteValue, rbyteValue, gbyteValue, bbyteValue);
            return retVal;
        }

        /// <summary>
        /// Validate the value of the text control
        /// </summary>
        /// <param name="e"> Event arguments</param>
        private void NumericValidation(KeyEventArgs e){
            string input;

            input = e.Key.ToString();
            if (e.Key == Key.Enter) {
                if (MakeColorFromRGB(out Color color)) {
                    CustomColor = color;
                    Reposition();
                }
            } else {
                if (input.StartsWith("D")) {
                    input = input[1..];
                } else if (input.StartsWith("NumPad")) {
                    input = input[6..];
                }
                if (!int.TryParse(input, out int _)) {
                    e.Handled = true; 
                }
            }
        }

        /// <summary>
        /// Load the bitmap
        /// </summary>
        /// <param name="source"></param>
        /// <returns>
        /// Bitmap source
        /// </returns>
        public static BitmapSource LoadBitmap(System.Drawing.Bitmap source) 
            => System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(source.GetHbitmap(),
                                                                            IntPtr.Zero,
                                                                            Int32Rect.Empty,
                                                                            BitmapSizeOptions.FromEmptyOptions());


        /// <summary>
        /// Do the initial work
        /// </summary>
        private void InitialWork() {
            DefaultPicker.Items.Clear();
            CustomColors customColors = new();
            foreach (var item in customColors.SelectableColors) {
                DefaultPicker.Items.Add(item);
            }
            DefaultPicker.SelectionChanged += new SelectionChangedEventHandler(DefaultPicker_SelectionChanged);
        }

        /// <summary>
        /// Called when the selection of the default picker changed
        /// </summary>
        /// <param name="sender"> Sender object</param>
        /// <param name="e">      Event arguments</param>
        private void DefaultPicker_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (DefaultPicker.SelectedValue != null) {
                m_customColor = (Color)DefaultPicker.SelectedValue;
            }
            FrameworkElement frameworkElement = this;
            while (true) {
                if (frameworkElement is ContextMenu ctxMenu) {
                    ctxMenu.IsOpen = false;
                    break;
                }
                if (frameworkElement.Parent is FrameworkElement fwElem) {
                    frameworkElement = fwElem;
                } else {
                    break;
                }
            }
        }

        /// <summary>
        /// Change the color
        /// </summary>
        private void ChangeColor() {
            try {
                CustomColor = GetColorFromImage((int)Mouse.GetPosition(CanColor).X, (int)Mouse.GetPosition(CanColor).Y);                
                MovePointer();                 
            } catch  {}
        }     

        private void Reposition() {

            for (int i = 0; i < CanColor.ActualWidth; i++) {
                bool flag = false;
                for (int j = 0; j < CanColor.ActualHeight; j++) {
                    try {
                        Color Colorfromimagepoint = GetColorFromImage(i, j);
                        if (SimmilarColor(Colorfromimagepoint, m_customColor)) {
                            MovePointerDuringReposition(i, j);
                            flag = true;
                            break;
                        }
                    }
                    catch {}
                }
                if (flag) break;
            }
        }

        /// <summary>
        /// 1*1 pixel copy is based on an article by Lee Brimelow    
        /// http://thewpfblog.com/?p=62
        /// </summary>
        private Color GetColorFromImage(int i, int j) {
            CroppedBitmap cb = new(image.Source as BitmapSource, new Int32Rect(i, j, 1, 1));
            byte[] color = new byte[4];
            cb.CopyPixels(color, 4, 0);
            Color Colorfromimagepoint = Color.FromArgb((byte)SdA.Value, color[2], color[1], color[0]);
            return Colorfromimagepoint;
        }

        /// <summary>
        /// Move the pointer during reposition
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        private void MovePointerDuringReposition(int i, int j){
            EpPointer.SetValue(Canvas.LeftProperty, (double)(i - 3));
            EpPointer.SetValue(Canvas.TopProperty, (double)(j - 3));
            EpPointer.InvalidateVisual();
            CanColor.InvalidateVisual();
        }

        /// <summary>
        /// Move the pointer
        /// </summary>
        private void MovePointer() {
            EpPointer.SetValue(Canvas.LeftProperty, (double)(Mouse.GetPosition(CanColor).X - 5));
            EpPointer.SetValue(Canvas.TopProperty,  (double)(Mouse.GetPosition(CanColor).Y - 5));
            CanColor.InvalidateVisual();
        }

        /// <summary>
        /// Determine if the two color are similar
        /// </summary>
        /// <param name="pointColor">   Color pointed</param>
        /// <param name="selectedColor">Selected color</param>
        /// <returns></returns>
        private static bool SimmilarColor(Color pointColor, Color selectedColor)
            => Math.Abs(pointColor.R - selectedColor.R) + Math.Abs(pointColor.G - selectedColor.G) + Math.Abs(pointColor.B - selectedColor.B) < 20;

        /// <summary>
        /// Update the preview
        /// </summary>
        private void UpdatePreview() {
            lblPreview.Background   = new SolidColorBrush(CustomColor);
            txtAlpha.Text           = CustomColor.A.ToString();
            string alphaHex         = CustomColor.A.ToString("X").PadLeft(2, '0');
            txtR.Text               = CustomColor.R.ToString();
            string redHex           = CustomColor.R.ToString("X").PadLeft(2, '0');
            txtG.Text               = CustomColor.G.ToString();
            string greenHex         = CustomColor.G.ToString("X").PadLeft(2, '0');
            txtB.Text               = CustomColor.B.ToString();
            string blueHex          = CustomColor.B.ToString("X").PadLeft(2, '0');
            txtAll.Text             = string.Format("#{0}{1}{2}{3}", alphaHex, redHex, greenHex, blueHex);
            SdA.Value               = CustomColor.A;
        }

        /// <summary>
        /// Called when the left mouse button is down
        /// </summary>
        /// <param name="sender"> Sender object</param>
        /// <param name="e">      Event arguments</param>
        private void EpDefaultcolor_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) => epCustomcolor.IsExpanded = false;


        /// <summary>
        /// Called when the custom color is expanded
        /// </summary>
        /// <param name="sender"> Sender object</param>
        /// <param name="e">      Event arguments</param>
        private void EpCustomcolor_Expanded(object sender, RoutedEventArgs e) => epDefaultcolor.IsExpanded = false;

        /// <summary>
        /// Called when the slier value changed
        /// </summary>
        /// <param name="sender"> Sender object</param>
        /// <param name="e">      Event arguments</param>
        private void SdA_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
            => CustomColor = Color.FromArgb((byte)SdA.Value, CustomColor.R, CustomColor.G, CustomColor.B);
    }

    /// <summary>
    /// Custom colors
    /// </summary>
    class CustomColors {
        public List<Color> SelectableColors { get; set; } = new List<Color>();

        public CustomColors() {
            Type            colorsType      = typeof(Colors);
            PropertyInfo[]  colorsProperty  = colorsType.GetProperties();

            foreach (PropertyInfo property in colorsProperty) {
                SelectableColors.Add((Color)ColorConverter.ConvertFromString(property.Name));
            }
        }

    }

    /// <summary>
    /// Converter
    /// </summary>
    [ValueConversion(typeof(Color), typeof(Brush))]
    public class ColorToSolidColorBrushConverter : IValueConverter {
        #region IValueConverter Members

        /// <summary>
        /// Convert
        /// </summary>
        /// <param name="value">      Value to convert</param>
        /// <param name="targetType"> Target type</param>
        /// <param name="parameter">  Parameter</param>
        /// <param name="culture">    Culture</param>
        /// <returns>
        /// Convert a color to a brush
        /// </returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => new SolidColorBrush((Color)value);

        /// <summary>
        /// Convert back
        /// </summary>
        /// <param name="value">      Value to convert</param>
        /// <param name="targetType"> Target type</param>
        /// <param name="parameter">  Parameter</param>
        /// <param name="culture">    Culture</param>
        /// <returns>Convert a brush to a color</returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => throw new NotImplementedException();

        #endregion
    }
}
