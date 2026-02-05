using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Media.Animation;
using Clipboard = System.Windows.Clipboard;
using SelectUnknown.LogManagement;

namespace SelectUnknown
{
    /// <summary>
    /// LensWindow.xaml 的交互逻辑
    /// </summary>
    public partial class LensWindow : Window
    {
        public LensWindow(BitmapSource scrImg)
        {
            InitializeComponent();
            ScreenImage.Source = scrImg;
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key) { 
                case Key.Escape:
                    BackgroundImage.Source = null;
                    ScreenImage.Source = null;
                    this.Close();
                    break;
                case Key.Back:
                    if (BackgroundImage.Opacity == BackgroundImgDefaultOpacity)
                    {
                        HideBackgroundImg(200);
                    }
                    else
                    {
                        ShowBackgroundImg(200);
                    }
                        break;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 淡入背景氛围图动画
            ShowBackgroundImg(300);
        }
        const double BackgroundImgDefaultOpacity = 0.3;
        /// <summary>
        /// 隐藏背景氛围图
        /// </summary>
        /// <param name="duration">动画持续时间(毫秒)</param>
        void HideBackgroundImg(int duration, double value = 0)
        {
            var animation = new DoubleAnimation
            {
                From = BackgroundImage.Opacity,
                To = value,
                Duration = TimeSpan.FromMilliseconds(duration),
                FillBehavior = FillBehavior.HoldEnd
            };

            BackgroundImage.BeginAnimation(UIElement.OpacityProperty, animation);
        }
        /// <summary>
        /// 显示背景氛围图
        /// </summary>
        /// <param name="duration">动画持续时间(毫秒)</param>
        /// <param name="value">目标设定值</param>
        void ShowBackgroundImg(int duration, double value = BackgroundImgDefaultOpacity)
        {
            var animation = new DoubleAnimation
            {
                From = BackgroundImage.Opacity,
                To = value,
                Duration = TimeSpan.FromMilliseconds(duration),
                FillBehavior = FillBehavior.HoldEnd
            };

            BackgroundImage.BeginAnimation(UIElement.OpacityProperty, animation);
        }

        private void SelectRectangle_Click(object sender, RoutedEventArgs e)
        {

        }
        #region 取色工具
        private void TakeColor_Click(object sender, RoutedEventArgs e)
        {
            LogHelper.Log("用户选择了取色工具");
            HideBackgroundImg(200, 0);// 隐藏背景图以便取色
            ScreenImage.Cursor = System.Windows.Input.Cursors.Pen;
            ScreenImage.PreviewMouseDown += TakeColorHex;
        }

        private void TakeColorHex(object sender, MouseButtonEventArgs e)
        {
            string colorHex = GetColorHexFromScreenImage();
            
            Main.CopyToClipboard(colorHex);

            ScreenImage.PreviewMouseDown -= TakeColorHex;
            ScreenImage.Cursor = System.Windows.Input.Cursors.Arrow;
            ShowBackgroundImg(200, BackgroundImgDefaultOpacity);// 恢复背景图
            LogHelper.Log($"取色完毕，颜色为 {colorHex}");
        }

        System.Windows.Point GetMousePositionOnImage()
        {
            return Mouse.GetPosition(ScreenImage);
        }
        System.Windows.Media.Color GetColorFromScreenImage()
        {
            if (ScreenImage.Source is not BitmapSource bmp)
                return Colors.Transparent;

            System.Windows.Point p = Mouse.GetPosition(ScreenImage);

            if (p.X < 0 || p.Y < 0 ||
                p.X > ScreenImage.ActualWidth ||
                p.Y > ScreenImage.ActualHeight)
                return Colors.Transparent;

            double imageAspect = (double)bmp.PixelWidth / bmp.PixelHeight;
            double controlAspect = ScreenImage.ActualWidth / ScreenImage.ActualHeight;

            double scale;
            double offsetX = 0;
            double offsetY = 0;

            if (ScreenImage.Stretch == Stretch.UniformToFill)
            {
                scale = controlAspect > imageAspect
                    ? ScreenImage.ActualWidth / bmp.PixelWidth
                    : ScreenImage.ActualHeight / bmp.PixelHeight;
            }
            else // Uniform / Fill / None
            {
                scale = controlAspect < imageAspect
                    ? ScreenImage.ActualWidth / bmp.PixelWidth
                    : ScreenImage.ActualHeight / bmp.PixelHeight;
            }

            double displayWidth = bmp.PixelWidth * scale;
            double displayHeight = bmp.PixelHeight * scale;

            offsetX = (ScreenImage.ActualWidth - displayWidth) / 2;
            offsetY = (ScreenImage.ActualHeight - displayHeight) / 2;

            double x = (p.X - offsetX) / scale;
            double y = (p.Y - offsetY) / scale;

            if (x < 0 || y < 0 ||
                x >= bmp.PixelWidth ||
                y >= bmp.PixelHeight)
                return Colors.Transparent;

            return GetPixelFromBitmapSource(bmp, (int)x, (int)y);
        }
        System.Windows.Media.Color GetPixelFromBitmapSource(BitmapSource bmp, int x, int y)
        {
            byte[] pixel = new byte[4];
            Int32Rect rect = new Int32Rect(x, y, 1, 1);

            bmp.CopyPixels(rect, pixel, 4, 0);

            return System.Windows.Media.Color.FromArgb(
                pixel[3], // A
                pixel[2], // R
                pixel[1], // G
                pixel[0]  // B
            );
        }
        string GetColorHexFromScreenImage()
        {
            var c = GetColorFromScreenImage();
            return $"#{c.R:X2}{c.G:X2}{c.B:X2}";
        }
        #endregion
        #region 工具栏拖动
        private System.Windows.Point _startPoint;
        private System.Windows.Point _lastMouseDown;
        private bool _isDragging;
        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = true;
            _lastMouseDown = e.GetPosition(this); // 获取相对于窗口/父容器的坐标
            ((UIElement)sender).CaptureMouse();
        }

        private void Border_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            ((UIElement)sender).ReleaseMouseCapture();
        }
        
        private void Border_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_isDragging)
            {
                System.Windows.Point currentPosition = e.GetPosition(this);
                // 计算鼠标位移量
                double offsetX = currentPosition.X - _lastMouseDown.X;
                double offsetY = currentPosition.Y - _lastMouseDown.Y;

                // 更新平移变换
                dragTransform.X += offsetX;
                dragTransform.Y += offsetY;

                _lastMouseDown = currentPosition;
            }
        }
        #endregion
    }
}
