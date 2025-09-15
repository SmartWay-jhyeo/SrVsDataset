using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Interop;

namespace SrVsDataset.Controls
{
    /// <summary>
    /// 이미지 줌/드래그 기능을 제공하는 UserControl
    /// </summary>
    public partial class ImageViewer : System.Windows.Controls.UserControl
    {
        public static readonly DependencyProperty ImageSourceProperty =
            DependencyProperty.Register("ImageSource", typeof(ImageSource), typeof(ImageViewer), 
                new PropertyMetadata(null));

        public ImageSource ImageSource
        {
            get { return (ImageSource)GetValue(ImageSourceProperty); }
            set { SetValue(ImageSourceProperty, value); }
        }

        private double _zoomFactor = 1.0;
        public double ZoomFactor => _zoomFactor;

        public ImageViewer()
        {
            InitializeComponent();
            
            // 더블 버퍼링 활성화로 깜빡임 방지
            RenderOptions.SetBitmapScalingMode(PreviewImage, BitmapScalingMode.NearestNeighbor);
            RenderOptions.SetEdgeMode(PreviewImage, EdgeMode.Aliased);
            
            // 마우스 휠 이벤트 처리
            PreviewImage.MouseWheel += OnMouseWheel;
            PreviewImage.MouseLeftButtonDown += OnMouseLeftButtonDown;
            PreviewImage.MouseMove += OnMouseMove;
            PreviewImage.MouseLeftButtonUp += OnMouseLeftButtonUp;
        }

        private System.Windows.Point _lastPanPoint;
        private bool _isPanning = false;

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Ctrl 키를 누른 상태에서 마우스 휠로 줌 조절
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                var delta = e.Delta > 0 ? 1.1 : 0.9;
                _zoomFactor *= delta;
                _zoomFactor = Math.Max(0.1, Math.Min(_zoomFactor, 10.0));
                
                var transform = new ScaleTransform(_zoomFactor, _zoomFactor);
                PreviewImage.RenderTransform = transform;
                
                e.Handled = true;
            }
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_zoomFactor > 1.0)
            {
                _isPanning = true;
                _lastPanPoint = e.GetPosition(ImageScrollViewer);
                PreviewImage.CaptureMouse();
                e.Handled = true;
            }
        }

        private void OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_isPanning && e.LeftButton == MouseButtonState.Pressed)
            {
                var currentPoint = e.GetPosition(ImageScrollViewer);
                var deltaX = currentPoint.X - _lastPanPoint.X;
                var deltaY = currentPoint.Y - _lastPanPoint.Y;

                ImageScrollViewer.ScrollToHorizontalOffset(ImageScrollViewer.HorizontalOffset - deltaX);
                ImageScrollViewer.ScrollToVerticalOffset(ImageScrollViewer.VerticalOffset - deltaY);

                _lastPanPoint = currentPoint;
                e.Handled = true;
            }
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isPanning)
            {
                _isPanning = false;
                PreviewImage.ReleaseMouseCapture();
                e.Handled = true;
            }
        }

        /// <summary>
        /// 줌을 1:1로 재설정
        /// </summary>
        public void ResetZoom()
        {
            _zoomFactor = 1.0;
            PreviewImage.RenderTransform = new ScaleTransform(1.0, 1.0);
        }

        /// <summary>
        /// 이미지를 화면에 맞게 조정
        /// </summary>
        public void FitToScreen()
        {
            if (ImageSource is BitmapSource bitmap)
            {
                var scaleX = ActualWidth / bitmap.PixelWidth;
                var scaleY = ActualHeight / bitmap.PixelHeight;
                var scale = Math.Min(scaleX, scaleY);
                
                _zoomFactor = scale;
                PreviewImage.RenderTransform = new ScaleTransform(scale, scale);
            }
        }
    }
}