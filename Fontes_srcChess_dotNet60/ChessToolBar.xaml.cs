using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.ComponentModel;

namespace SrcChess2 {
    /// <summary>
    /// Toolbar for the Chess Program
    /// </summary>
    public partial class ChessToolBar {
        
        /// <summary>
        /// Class Ctor
        /// </summary>
        public ChessToolBar() {
            InitializeComponent();
            ProgressBar.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// Start the progress bar
        /// </summary>
        public void StartProgressBar() {
            ProgressBar.Visibility = Visibility.Visible;
            ProgressBar.Start();
        }

        /// <summary>
        /// Stop the progress bar
        /// </summary>
        public void EndProgressBar() {
            ProgressBar.Stop();
            ProgressBar.Visibility = Visibility.Hidden;
        }
    }

    /// <summary>
    /// Defines a toolbar button
    /// </summary>
    public class ToolBarButton : Button {
        /// <summary>Image dependency property</summary>
        public static readonly DependencyProperty ImageProperty;
        /// <summary>Image Disabled dependency property</summary>
        public static readonly DependencyProperty DisabledImageProperty;
        /// <summary>Flip dependency property</summary>
        public static readonly DependencyProperty FlipProperty;
        /// <summary>Image dependency property</summary>
        public static readonly DependencyProperty TextProperty;
        /// <summary>DisplayStyle dependency property</summary>
        public static readonly DependencyProperty DisplayStyleProperty;
        /// <summary>Inner Image control</summary>
        private Image?                            m_imageCtrl;
        /// <summary>Inner Text control</summary>
        private TextBlock?                        m_textCtrl;

        /// <summary>Display Style applied to the Toolbarbutton</summary>
        public enum TbDisplayStyle {
            /// <summary>Image only displayed</summary>
            Image,
            /// <summary>Text only displayed</summary>
            Text,
            /// <summary>Image and Text displayed</summary>
            ImageAndText
        }

        /// <summary>
        /// Class ctor
        /// </summary>
        static ToolBarButton() {
            ImageProperty         = DependencyProperty.Register("Image",
                                                                typeof(ImageSource),
                                                                typeof(ToolBarButton),
                                                                new FrameworkPropertyMetadata(defaultValue: null,
                                                                                              FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure,
                                                                                              ImageChanged));
            DisabledImageProperty = DependencyProperty.Register("DisabledImage",
                                                                typeof(ImageSource),
                                                                typeof(ToolBarButton),
                                                                new FrameworkPropertyMetadata(defaultValue: null,
                                                                                              FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure,
                                                                                              DisabledImageChanged));
            FlipProperty          = DependencyProperty.Register("Flip",
                                                                typeof(bool),
                                                                typeof(ToolBarButton),
                                                                new FrameworkPropertyMetadata(defaultValue: false,
                                                                                              FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure,
                                                                                              FlipChanged));
            TextProperty          = DependencyProperty.Register("Text",
                                                                typeof(string),
                                                                typeof(ToolBarButton),
                                                                new FrameworkPropertyMetadata(defaultValue: "",
                                                                                              FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.Inherits,
                                                                                              TextChanged));
            DisplayStyleProperty  = DependencyProperty.RegisterAttached("DisplayStyle",
                                                                       typeof(TbDisplayStyle),
                                                                       typeof(ToolBarButton),
                                                                       new FrameworkPropertyMetadata(defaultValue: TbDisplayStyle.Text,
                                                                                                     FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsParentArrange | FrameworkPropertyMetadataOptions.AffectsParentMeasure | FrameworkPropertyMetadataOptions.Inherits,
                                                                                                     DisplayStyleChanged));
            IsEnabledProperty.OverrideMetadata(typeof(ToolBarButton), new FrameworkPropertyMetadata(defaultValue: true, new PropertyChangedCallback(IsEnabledChanged)));
        }

        /// <summary>
        /// Class constructor
        /// </summary>
        public ToolBarButton() : base() {
            Style = new Style(typeof(ToolBarButton), (Style)FindResource(ToolBar.ButtonStyleKey));
            BuildInnerButton();
        }

        /// <summary>
        /// Called when Image property changed
        /// </summary>
        private static void ImageChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e) {
            if (obj is ToolBarButton me && e.OldValue != e.NewValue) {
                me.UpdateInnerButton();
            }
        }

        /// <summary>
        /// Image displayed to the button
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Browsable(true)]
        [Bindable(true)]
        [Category("Layout")]
        [Description("Image displayed in button")]
        public ImageSource Image {
            get => (ImageSource)GetValue(ImageProperty);
            set => SetValue(ImageProperty, value);
        }

        /// <summary>
        /// Called when Disabled Image property changed
        /// </summary>
        private static void DisabledImageChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e) {
            if (obj is ToolBarButton me && e.OldValue != e.NewValue) {
                me.UpdateInnerButton();
            }
        }

        /// <summary>
        /// Disabled Image displayed to the button
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Browsable(true)]
        [Bindable(true)]
        [Category("Layout")]
        [Description("Disabled Image displayed in button")]
        public ImageSource DisabledImage {
            get => (ImageSource)GetValue(DisabledImageProperty);
            set => SetValue(DisabledImageProperty, value);
        }

        /// <summary>
        /// Called when Flip property changed
        /// </summary>
        private static void FlipChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e) {
            if (obj is ToolBarButton me && e.OldValue != e.NewValue) {
                me.UpdateInnerButton();
            }
        }

        /// <summary>
        /// Flip the image horizontally
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Browsable(true)]
        [Bindable(true)]
        [Category("Layout")]
        [Description("Flip horizontally the Image displayed in button")]
        public bool Flip {
            get => (bool)GetValue(FlipProperty);
            set => SetValue(FlipProperty, value);
        }

        /// <summary>
        /// Called when Text property changed
        /// </summary>
        private static void TextChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e) {
            if (obj is ToolBarButton me && e.OldValue != e.NewValue) {
                me.UpdateInnerButton();
            }
        }

        /// <summary>
        /// Text displayed in button
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Browsable(true)]
        [Bindable(true)]
        [Category("Layout")]
        [Description("Text displayed in button")]
        public string Text {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        /// <summary>
        /// Called when DisplayStyle property changed
        /// </summary>
        private static void DisplayStyleChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e) {
            if (obj is ToolBarButton tbItem && e.OldValue != e.NewValue) {
                tbItem.UpdateInnerButton();
            }
        }

        /// <summary>
        /// Display Style applied to the button
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Browsable(true)]
        [Bindable(true)]
        [Category("Layout")]
        [Description("Display Style applied to the button")]
        public TbDisplayStyle DisplayStyle {
            get => (TbDisplayStyle)GetValue(DisplayStyleProperty);
            set => SetValue(DisplayStyleProperty, value);
        }

        /// <summary>
        /// Set the Display Style
        /// </summary>
        /// <param name="element">      Dependency element</param>
        /// <param name="displayStyle"> Display Style</param>
        public static void SetDisplayStyle(DependencyObject element, TbDisplayStyle displayStyle) {
            if (element == null) {
                throw new ArgumentNullException(nameof(element));
            }
            element.SetValue(DisplayStyleProperty, displayStyle);
        }

        /// <summary>
        /// Get the full name of the field attached to a column
        /// </summary>
        /// <param name="element">  Dependency element</param>
        /// <returns>
        /// Field full name
        /// </returns>
        public static TbDisplayStyle GetDisplayStyle(DependencyObject element) {
            if (element == null) {
                throw new ArgumentNullException(nameof(element));
            }
            return (TbDisplayStyle)element.GetValue(DisplayStyleProperty);
        }

        /// <summary>
        /// Called when IsEnabled property changed
        /// </summary>
        private new static void IsEnabledChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e) {
            if (obj is ToolBarButton me && e.OldValue != e.NewValue) {
                me.UpdateInnerButton();
            }
        }

        /// <summary>
        /// Set the source image depending the enabled state
        /// </summary>
        /// <param name="bFlip">    true if flipped</param>
        private void SetImage(bool bFlip) {
            ScaleTransform  scaleTransform;

            m_imageCtrl!.Source      = (IsEnabled) ? Image : DisabledImage;
            m_imageCtrl.OpacityMask = null;
            if (bFlip) {
                m_imageCtrl.RenderTransformOrigin = new Point(0.5, 0.5);
                scaleTransform = new ScaleTransform {
                    ScaleX = -1
                };
                m_imageCtrl.RenderTransform = scaleTransform;
            }
        }

        /// <summary>
        /// Builds the inner controls to make the button
        /// </summary>
        private void BuildInnerButton() {
            Grid grid;

            grid = new Grid {
                HorizontalAlignment = HorizontalAlignment.Center
            };
            m_imageCtrl = new Image();
            m_textCtrl  = new TextBlock() { Margin = new Thickness(5, 0, 0, 0), VerticalAlignment = System.Windows.VerticalAlignment.Center };
            grid.RowDefinitions.Add(new RowDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            Grid.SetColumn(m_imageCtrl, 0);
            grid.Children.Add(m_imageCtrl);
            Grid.SetColumn(m_textCtrl, 1);
            grid.Children.Add(m_textCtrl);
            Content = grid;
        }

        /// <summary>
        /// Updates the inner controls of the button
        /// </summary>
        private void UpdateInnerButton() {
            TbDisplayStyle displayStyle;
            Grid           grid;
            string         strText;

            grid         = (Grid)Content;
            displayStyle = DisplayStyle;
            strText      = Text;
            if (Image != null && (displayStyle == TbDisplayStyle.Image || displayStyle == TbDisplayStyle.ImageAndText)) {
                grid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
                SetImage(Flip);
            } else {
                m_imageCtrl!.Source             = null;
                grid.ColumnDefinitions[0].Width = new GridLength(0);
            }
            if (!string.IsNullOrEmpty(strText) && (displayStyle == TbDisplayStyle.Text || displayStyle == TbDisplayStyle.ImageAndText)) {
                grid.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);
                m_textCtrl!.Text                = strText;
            } else {
                m_textCtrl!.Text                = string.Empty;
                grid.ColumnDefinitions[1].Width = new GridLength(0);
            }
        } 
    }
}
