using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ZoomWpfTest
{
    /// <summary>
    /// Enables panning and zooming of a FrameworkElement within the ContentControl, via mouse.
    /// </summary>
    /// <remarks>
    /// Original PanAndZoomViewer code from Joe Wood at
    /// http://blogs.windowsclient.net/joeyw/archive/2009/06/02/pan-and-zoom-updated.aspx
    /// </remarks>
    public class PanAndZoomViewer : ContentControl, INotifyPropertyChanged
    {
        private Point _screenStartPoint = new Point(0, 0);
        private Point _startOffset;
        private TransformGroup _transformGroup;
        private TranslateTransform _translateTransform;
        private ScaleTransform _zoomTransform;
        public event PropertyChangedEventHandler PropertyChanged;
        public PanAndZoomViewer()
        {
            DefaultZoomFactor = 1.4;
            MaximumZoom = double.MaxValue;
            MinimumZoom = double.MinValue;
        }

        public Point ScreenStartPoint
        {
            get { return _screenStartPoint; }
            set
            {
                _screenStartPoint = value;
                OnPropertyChanged("ScreenStartPoint");
            }
        }

        public Point StartOffset
        {
            get { return _startOffset; }
            set
            {
                _startOffset = value;
                OnPropertyChanged("StartOffset");
            }
        }

        public TranslateTransform TranslateTransform
        {
            get { return _translateTransform; }
            set
            {
                _translateTransform = value;
                OnPropertyChanged("TranslateTransform");
            }
        }

        public double DefaultZoomFactor { get; set; }
        public double MaximumZoom { get; set; }
        public double MinimumZoom { get; set; }

        public ScaleTransform ZoomTransform
        {
            get { return _zoomTransform; }
            set
            {
                _zoomTransform = value;
                OnPropertyChanged("ZoomTransform");
            }
        }

        public TransformGroup TransformGroup
        {
            get { return _transformGroup; }
            set
            {
                _transformGroup = value;
                OnPropertyChanged("TransformGroup");
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            Setup();
        }

        private void Setup()
        {
            _translateTransform = new TranslateTransform();
            _zoomTransform = new ScaleTransform();
            TransformGroup = new TransformGroup();
            TransformGroup.Children.Add(_zoomTransform);
            TransformGroup.Children.Add(_translateTransform);
            Focusable = true;
            KeyDown += source_KeyDown;
            MouseMove += control_MouseMove;
            MouseRightButtonDown += source_MouseRightButtonDown;
            MouseRightButtonUp += source_MouseRightButtonUp;
            MouseWheel += source_MouseWheel;
        }

        private void source_KeyDown(object sender, KeyEventArgs e)
        {
            // hit escape to reset everything
            if (e.Key == Key.Escape) Reset();
        }

        private void source_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            // zoom into the content.  Calculate the zoom factor based on the direction of the mouse wheel.
            double zoomFactor = DefaultZoomFactor;
            if (e.Delta <= 0) zoomFactor = 1.0 / DefaultZoomFactor;
            // DoZoom requires both the logical and physical location of the mouse pointer
            Point physicalPoint = e.GetPosition(this);
            DoZoom(zoomFactor, TransformGroup.Inverse.Transform(physicalPoint), physicalPoint);

        }

        private void source_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (IsMouseCaptured)
            {
                // we're done.  reset the cursor and release the mouse pointer
                Cursor = Cursors.Arrow;
                ReleaseMouseCapture();
            }
        }

        private void source_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Save starting point, used later when determining how much to scroll.
            _screenStartPoint = e.GetPosition(this);
            _startOffset = new Point(_translateTransform.X, _translateTransform.Y);
            CaptureMouse();
            Cursor = Cursors.ScrollAll;
        }


        private void control_MouseMove(object sender, MouseEventArgs e)
        {
            if (!IsMouseCaptured)
            {
                return; // don't care.
            }

            // if the mouse is captured then move the content by changing the translate transform.  
            // use the Pan Animation to animate to the new location based on the delta between the 
            // starting point of the mouse and the current point.
            Point physicalPoint = e.GetPosition(this);

            // where you'd like to move the top left corner of the view to
            double toX = physicalPoint.X - _screenStartPoint.X + _startOffset.X;
            double toY = physicalPoint.Y - _screenStartPoint.Y + _startOffset.Y;
          

            _translateTransform.BeginAnimation(TranslateTransform.XProperty, CreatePanAnimation(toX),
                                              HandoffBehavior.Compose);
            _translateTransform.BeginAnimation(TranslateTransform.YProperty, CreatePanAnimation(toY),
                                      HandoffBehavior.Compose);

        }


        /// <summary>Helper to create the panning animation for x,y coordinates.</summary>
        /// <param name="toValue">New value of the coordinate.</param>
        /// <returns>Double animation</returns>
        private static DoubleAnimation CreatePanAnimation(double toValue)
        {
            var da = new DoubleAnimation(toValue, new Duration(TimeSpan.FromMilliseconds(300)))
                         {
                             AccelerationRatio = 0.1,
                             DecelerationRatio = 0.9,
                             FillBehavior = FillBehavior.HoldEnd
                         };
            da.Freeze();
            return da;
        }


        /// <summary>Helper to create the zoom double animation for scaling.</summary>
        /// <param name="toValue">Value to animate to.</param>
        /// <returns>Double animation.</returns>
        private static DoubleAnimation CreateZoomAnimation(double toValue)
        {
            var da = new DoubleAnimation(toValue, new Duration(TimeSpan.FromMilliseconds(500)))
                         {
                             AccelerationRatio = 0.1,
                             DecelerationRatio = 0.9,
                             FillBehavior = FillBehavior.HoldEnd
                         };
            da.Freeze();
            return da;
        }

        /// <summary>Zoom into or out of the content.</summary>
        /// <param name="deltaZoom">Factor to mutliply the zoom level by. </param>
        /// <param name="mousePosition">Logical mouse position relative to the original content.</param>
        /// <param name="physicalPosition">Actual mouse position on the screen (relative to the parent window)</param>
        public void DoZoom(double deltaZoom, Point mousePosition, Point physicalPosition)
        {
            // Keep Zoom within bounds declared by Minimum/MaximumZoom
            double currentZoom = _zoomTransform.ScaleX;
            currentZoom *= deltaZoom;
            if (currentZoom < MinimumZoom)
                currentZoom = MinimumZoom;
            else if (currentZoom > MaximumZoom)
                currentZoom = MaximumZoom;

            _translateTransform.BeginAnimation(TranslateTransform.XProperty,
                                              CreateZoomAnimation(-1 *
                                                                  (mousePosition.X * currentZoom - physicalPosition.X)));
            _translateTransform.BeginAnimation(TranslateTransform.YProperty,
                                              CreateZoomAnimation(-1 *
                                                                  (mousePosition.Y * currentZoom - physicalPosition.Y)));
            _zoomTransform.BeginAnimation(ScaleTransform.ScaleXProperty, CreateZoomAnimation(currentZoom));
            _zoomTransform.BeginAnimation(ScaleTransform.ScaleYProperty, CreateZoomAnimation(currentZoom));
        }

        /// <summary>Reset to default zoom level and centered content.</summary>
        public void Reset()
        {
            _translateTransform.BeginAnimation(TranslateTransform.XProperty, CreateZoomAnimation(0));
            _translateTransform.BeginAnimation(TranslateTransform.YProperty, CreateZoomAnimation(0));
            _zoomTransform.BeginAnimation(ScaleTransform.ScaleXProperty, CreateZoomAnimation(1));
            _zoomTransform.BeginAnimation(ScaleTransform.ScaleYProperty, CreateZoomAnimation(1));
        }

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}