using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Input;
using System.Windows.Shapes;

namespace SrcChess2 {
    /// <summary>
    /// A circular type progress bar, that is simliar to popular web based progress bars
    /// Modified version of WPF Circular Progress Bar from Sacha Barber
    /// </summary>
    public partial class CircularProgressBar {
        /// <summary>Animation timer</summary>
        private readonly DispatcherTimer m_animationTimer;

        /// <summary>
        /// Ctor
        /// </summary>
        public CircularProgressBar() {
            InitializeComponent();
            m_animationTimer = new DispatcherTimer(DispatcherPriority.ContextIdle, Dispatcher) {
                Interval = TimeSpan.FromMilliseconds(500)
            };
        }

        /// <summary>
        /// Animation speed in ms
        /// </summary>
        public int SpeedInMS {
            get => (int)m_animationTimer.Interval.TotalMilliseconds;
            set => m_animationTimer.Interval = TimeSpan.FromMilliseconds(value);
        }

        /// <summary>
        /// Start the animation
        /// </summary>
        public void Start() {
            Mouse.OverrideCursor   = Cursors.Wait;
            m_animationTimer.Tick += HandleAnimationTick;
            m_animationTimer.Start();
        }

        /// <summary>
        /// Stopping the animation
        /// </summary>
        public void Stop() {
            m_animationTimer.Stop();
            Mouse.OverrideCursor   = Cursors.Arrow;
            m_animationTimer.Tick -= HandleAnimationTick;
        }

        /// <summary>
        /// Call for each tick
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event argument</param>
        private void HandleAnimationTick(object? sender, EventArgs e) => SpinnerRotate.Angle = (SpinnerRotate.Angle + 18) % 360;

        /// <summary>
        /// Called when control is loaded
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event arguments</param>
        private void HandleLoaded(object sender, RoutedEventArgs e) {
            const double offset = Math.PI;
            const double step   = Math.PI * 2 / 10.0;

            if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(this)) {
                SetPosition(C0, offset, 0.0, step);
                SetPosition(C1, offset, 1.0, step);
                SetPosition(C2, offset, 2.0, step);
                SetPosition(C3, offset, 3.0, step);
                SetPosition(C4, offset, 4.0, step);
                SetPosition(C5, offset, 5.0, step);
                SetPosition(C6, offset, 6.0, step);
                SetPosition(C7, offset, 7.0, step);
                SetPosition(C8, offset, 8.0, step);
                SetPosition(C9, offset, 9.0, step);
            }
        }

        /// <summary>
        /// Set the dot position
        /// </summary>
        /// <param name="ellipse">   Ellipse</param>
        /// <param name="offset">    Offset</param>
        /// <param name="posOffSet"> Position offset</param>
        /// <param name="step">      Step</param>
        private static void SetPosition(Ellipse ellipse, double offset, double posOffSet, double step) {
            ellipse.SetValue(Canvas.LeftProperty, 50.0 + Math.Sin(offset + posOffSet * step) * 50.0);
            ellipse.SetValue(Canvas.TopProperty,  50.0 + Math.Cos(offset + posOffSet * step) * 50.0);
        }


        /// <summary>
        /// Called when control is unloaded
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event argument</param>
        private void HandleUnloaded(object sender, RoutedEventArgs e) => Stop();

    }
}