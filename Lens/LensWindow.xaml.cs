using Microsoft.Web.WebView2.Core;
using SelectUnknown.ConfigManagment;
using SelectUnknown.Lens;
using SelectUnknown.LogManagement;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static System.Net.WebRequestMethods;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;
using Clipboard = System.Windows.Forms.Clipboard;
using Path = System.IO.Path;

namespace SelectUnknown
{
    /// <summary>
    /// LensWindow.xaml 的交互逻辑
    /// </summary>
    public partial class LensWindow : Window
    {
        string searchUri = Main.GetSEHomeUrl();
        Bitmap screenImg;
        public LensWindow(Bitmap scrImg, string selectedWords = "")
        {
            InitializeComponent();
            BitmapSource bmps = LensHelper.ConvertToBitmapSource(scrImg);
            ScreenImage.Source = bmps;
            if (!string.IsNullOrWhiteSpace(selectedWords))
            {
                searchUri = Main.GetSESearchingUrl(selectedWords);
                MainText.Text = selectedWords;
            }

            Clipboard.SetDataObject(bmps);
            
            screenImg = scrImg;
        }
       
        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key) { 
                case Key.Escape:
                    ShutdownWindow();
                    break;
                case Key.Back:
                    HideBackground_Click(sender, e);
                    break;
                case Key.Tab:
                    if (Browser.Visibility == Visibility.Visible)
                    {
                        Browser.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        Browser.Visibility = Visibility.Visible;
                    }
                    break;
            }
        }
        bool closed = false;
        /// <summary>
        /// 关闭窗口并释放一定资源
        /// </summary>
        private void ShutdownWindow()
        {
            if (closed) return;
            BackgroundImage.Source = null;
            BackgroundImage.UpdateLayout();
            ScreenImage.Source = null;
            ScreenImage.UpdateLayout();
            this.DataContext = null;
            if (webView != null && webView.CoreWebView2 != null)//没有就不用释放
            {
                webView.Dispose();
                webView = null;
            }
            screenImg.Dispose();
            screenImg = null;
            ShutdownSelectRectangleMode();

            //取消订阅事件
            ScreenImage.PreviewMouseDown -= TakeColorHex;
            ShowInBrowser.Click -= ShowInBrowser_Click;
            Minisize.Click -= Minisize_Click;
            this.Loaded -= Window_Loaded;
            SelectRectangle.Click -= SelectRectangle_Click;
            if (webView != null && webView.CoreWebView2 != null)
            {
                webView.CoreWebView2.NewWindowRequested -= webView_NewWindowRequested;
            }
            TakeColor.Click -= TakeColor_Click;

            closed = true;// 不这样做会导致再被调用一次,“鞭尸”会导致软件崩溃
            this.Close();
            MainGrid.Children.Clear();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            LogHelper.Log("框定即搜窗口已关闭，资源已释放");
        }
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 淡入背景氛围图动画
            ShowBackgroundImg(300);

            ShowLensControls(300);
            await webView.EnsureCoreWebView2Async();
            if (ConfigManagment.Config.UsingAndroidUserAgent)
            {
                webView.CoreWebView2.Settings.UserAgent = Main.GetWebViewUserAgent();//设置为安卓 UA
                LogHelper.Log("已设置 WebView2 使用安卓用户代理");
            }
            SelectRectangle_Click(sender, e);
        }
        private void webView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            Loading.Visibility = Visibility.Collapsed;
            if (webView != null && webView.CoreWebView2 != null)
            {
                webView.CoreWebView2.NewWindowRequested += webView_NewWindowRequested;
            }
        }
        bool isFirstNavigation = true;
        Int16 navigationTimes = 0;
        private void webView_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            Loading.Visibility = Visibility.Visible;
            navigationTimes++;
            if (isFirstNavigation)
            {
                webView_FirstNavigationStarting(sender, e);
            }
            isFirstNavigation = false;
            if (webView != null && webView.CoreWebView2 != null)
            {

            }
            if (isLensSearching && (navigationTimes - lensTimes) / 2 > 1)//我也不知道为什么要除以2
            {
                currentLensUrl = "";
                isLensSearching = false;
            }
        }
        /// <summary>
        /// webView加载完成时调用
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void webView_FirstNavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            if (webView != null && webView.CoreWebView2 != null)
            {
                webView.CoreWebView2.Navigate(searchUri);
            }
            searchUri = "";
        }
        private void webView_NewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            // 获取新窗口尝试加载的URL
            string newUrl = e.Uri;

            // 1. 将原窗口重定向到新URL
            webView.CoreWebView2.Navigate(newUrl);

            // 2. 标记该事件已被处理，阻止新建窗口/标签页
            e.Handled = true;
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
        /// <summary>
        /// 淡入控件
        /// </summary>
        /// <param name="duration"></param>
        /// <param name="value"></param>
        void ShowLensControls(int duration)
        {
            TextProcess.Opacity = 0.0;
            Browser.Opacity = 0.0;
            ToolBox.Opacity = 0.0;
            var animation = new DoubleAnimation
            {
                From = 0.0,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(duration),
                FillBehavior = FillBehavior.HoldEnd
            };

            TextProcess.BeginAnimation(UIElement.OpacityProperty, animation);
            Browser.BeginAnimation(UIElement.OpacityProperty, animation);
            ToolBox.BeginAnimation(UIElement.OpacityProperty, animation);
        }
        #region 框选工具
        System.Windows.Point startPoint;
        private void SelectRectangle_Click(object sender, RoutedEventArgs e)
        {
            LogHelper.Log("用户选择了框选工具");
            ScreenImage.Cursor = System.Windows.Input.Cursors.Cross;
            ScreenImage.MouseLeftButtonDown += StartSelectRectangle;
            ScreenImage.MouseMove += MovingSelectRectangle;
            ScreenImage.MouseLeftButtonUp += EndSelectRectangle;
            SelectionCanvas.MouseLeftButtonDown += StartSelectRectangle;
            SelectionCanvas.MouseMove += MovingSelectRectangle;
            SelectionCanvas.MouseLeftButtonUp += EndSelectRectangle;
        }
        private void StartSelectRectangle(object sender, MouseButtonEventArgs e)
        {
            startPoint = e.GetPosition(SelectionCanvas);
            SelectionRectangle.Width = 0;
            SelectionRectangle.Height = 0;
            SelectionRectangle.Visibility = Visibility.Visible;
            Canvas.SetLeft(SelectionRectangle, startPoint.X);
            Canvas.SetTop(SelectionRectangle, startPoint.Y);
        }
        private void MovingSelectRectangle(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var pos = e.GetPosition(SelectionCanvas);
                // 计算矩形位置和大小，支持反向拖拽
                var x = Math.Min(pos.X, startPoint.X);
                var y = Math.Min(pos.Y, startPoint.Y);
                var w = Math.Abs(pos.X - startPoint.X);
                var h = Math.Abs(pos.Y - startPoint.Y);

                SelectionRectangle.Width = w;
                SelectionRectangle.Height = h;
                Canvas.SetLeft(SelectionRectangle, x);
                Canvas.SetTop(SelectionRectangle, y);
            }
        }
        /// <summary>
        /// 判断是否正在使用 Lens 进行搜索，决定是否跳转至 currentLensUrl
        /// </summary>
        bool isLensSearching = false;
        string currentLensUrl = "";
        Int16 lensTimes = 0;
        private async void EndSelectRectangle(object sender, MouseButtonEventArgs e)
        {
            Bitmap croppedImg = GetSelectedImg();
            if (croppedImg == null) return;
            ImageToLens(croppedImg);
            if (croppedImg.Height >= 500 && croppedImg.Width >= 500)
            {
                Main.MousePopup("区域过大，无法识别");
                return;
            }
            string croppedTxt;
            TextProcessLoading.Visibility = Visibility.Visible;
            try
            {
                croppedTxt = await OCRHelper.RecognizeAsync(croppedImg);
            }
            catch (Exception ex)
            {
                LogHelper.Log($"文字识别时出错: {ex.Message}", LogLevel.Error);
                Main.MousePopup("文字识别出错，请重试");
                return;
            }
            TextProcessLoading.Visibility = Visibility.Hidden;

            if (!string.IsNullOrEmpty(croppedTxt))
            {
                MainText.Text = croppedTxt;
            }
            else
            {
                Main.MousePopup("文字识别失败");
            }
        }
        /// <summary>
        /// 分析图片
        /// </summary>
        /// <param name="bmp"></param>
        /// <returns></returns>
        public async void ImageToLens(Bitmap croppedImg)
        {
            Loading.Visibility = Visibility.Visible;
            string imageUrl = await LitterboxUploader.SendImageToLitterboxAndGetUrl(croppedImg);
            if (string.IsNullOrEmpty(imageUrl))
            {
                Main.MousePopup("图片上传失败，请检测网络，然后重试");
                Loading.Visibility = Visibility.Collapsed;
                LogHelper.Log("图片上传至 Litterbox 失败，无法使用分析", LogLevel.Error);
            }
            else
            {
                if (webView == null || webView.CoreWebView2 == null) return;
                currentLensUrl = Main.GetLensEngineUrl(imageUrl);
                webView.CoreWebView2.Navigate(currentLensUrl);
                isLensSearching = true;
                lensTimes = navigationTimes;
                LogHelper.Log($"用户完成了一次框选并上传至 {Config.LensEngineName} 进行分析");
            }
            Clipboard.SetImage(croppedImg);
        }
        private Bitmap GetSelectedImg()
        {
            var source = ScreenImage.Source as BitmapSource;
            if (source == null)
            {
                LogHelper.Log($"裁剪图片时出错: source为空", LogLevel.Error);
                Main.MousePopup("裁剪图片时出错，请重试");
                return screenImg;
            }

            // 1. 计算缩放比例 (假设图片是 Uniform 填充)
            double scaleX = source.PixelWidth / ScreenImage.ActualWidth;
            double scaleY = source.PixelHeight / ScreenImage.ActualHeight;

            // 2. 获取矩形在控件上的位置
            double rectX = Canvas.GetLeft(SelectionRectangle);
            double rectY = Canvas.GetTop(SelectionRectangle);

            // 3. 映射到原始像素坐标
            Int32Rect sourceRect = new Int32Rect(
                (int)(rectX * scaleX), (int)(rectY * scaleY),
                (int)(SelectionRectangle.Width * scaleX), (int)(SelectionRectangle.Height * scaleY));

            // 4. 使用 CroppedBitmap 裁剪提取
            try
            {
                var croppedBitmap = new CroppedBitmap(source, sourceRect);
                return Main.CroppedBitmapToBmp(croppedBitmap);
            }
            catch (Exception ex) 
            { 
                LogHelper.Log($"裁剪图片时出错: {ex.Message}", LogLevel.Error);
                Main.MousePopup("裁剪图片时出错，请重试");
                return screenImg;
            }
        }
        private void ShutdownSelectRectangleMode(bool doNotCloseRectangle = false)
        {
            ScreenImage.Cursor = System.Windows.Input.Cursors.Arrow;
            ScreenImage.MouseLeftButtonDown -= StartSelectRectangle;
            ScreenImage.MouseMove -= MovingSelectRectangle;
            ScreenImage.MouseLeftButtonUp -= EndSelectRectangle;
            SelectionCanvas.MouseLeftButtonDown -= StartSelectRectangle;
            SelectionCanvas.MouseMove -= MovingSelectRectangle;
            SelectionCanvas.MouseLeftButtonUp -= EndSelectRectangle;
            if (doNotCloseRectangle) return;
            // 隐藏矩形
            SelectionRectangle.Visibility = Visibility.Collapsed;

            // 将尺寸归零，防止逻辑干扰
            SelectionRectangle.Width = 0;
            SelectionRectangle.Height = 0;
        }
        #endregion
        #region 取色工具
        private void TakeColor_Click(object sender, RoutedEventArgs e)
        {
            LogHelper.Log("用户选择了取色工具");
            ShutdownSelectRectangleMode();// 取消框选
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
            Main.MousePopup($"已复制颜色值 {colorHex} 到剪贴板", 1200);
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
        private void ShowInBrowser_Click(object sender, RoutedEventArgs e)
        {
            string url;
            if (isLensSearching && !string.IsNullOrEmpty(currentLensUrl))
            {
                url = currentLensUrl;
            }
            else
            {
                url = AddressBar.Text;
            }
            Main.OpenUrl(url);
            ShutdownWindow();
        }
        protected override void OnClosed(EventArgs e)
        {
            ShutdownWindow();
            base.OnClosed(e);
        }
        private void Minisize_Click(object sender, RoutedEventArgs e)
        {
            Browser.Visibility = Visibility.Collapsed;
            Main.MousePopup("按 Tab 键可恢复显示", 1200);
        }
        #region 浏览器拖动
        private System.Windows.Point _lastMouseDown1;
        private bool _isDragging1;
        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging1 = true;
            _lastMouseDown1 = e.GetPosition(this); // 获取相对于窗口/父容器的坐标
            ((UIElement)sender).CaptureMouse();
        }

        private void Border_MouseMove_1(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_isDragging1)
            {
                System.Windows.Point currentPosition = e.GetPosition(this);
                // 计算鼠标位移量
                double offsetX = currentPosition.X - _lastMouseDown1.X;
                double offsetY = currentPosition.Y - _lastMouseDown1.Y;

                // 更新平移变换
                BrowserDragTransform.X += offsetX;
                BrowserDragTransform.Y += offsetY;
                ////给 Webview 的也更新一下，不然不会跟着跑 //不行，根本没用, 反会出现偏移
                //WebviewDragTransform.X += offsetX;
                //WebviewDragTransform.Y += offsetY;
                webView.InvalidateVisual();
                webView.UpdateLayout();
                Browser.InvalidateVisual();
                Browser.UpdateLayout();

                _lastMouseDown1 = currentPosition;
            }
        }

        private void Border_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging1 = false;
            ((UIElement)sender).ReleaseMouseCapture();
        }
        #endregion
        #region 文本处理框拖动
        private System.Windows.Point _lastMouseDownText;
        private bool _isDraggingText;
        private void Border_MouseDown_1(object sender, MouseButtonEventArgs e)
        {
            _isDraggingText = true;
            _lastMouseDownText = e.GetPosition(this); // 获取相对于窗口/父容器的坐标
            ((UIElement)sender).CaptureMouse();
        }

        private void Border_MouseMove_2(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_isDraggingText)
            {
                System.Windows.Point currentPosition = e.GetPosition(this);
                // 计算鼠标位移量
                double offsetX = currentPosition.X - _lastMouseDownText.X;
                double offsetY = currentPosition.Y - _lastMouseDownText.Y;

                // 更新平移变换
                TextProcessDragTransform.X += offsetX;
                TextProcessDragTransform.Y += offsetY;

                _lastMouseDownText = currentPosition;
            }
        }

        private void Border_MouseUp_1(object sender, MouseButtonEventArgs e)
        {
            _isDraggingText = false;
            ((UIElement)sender).ReleaseMouseCapture();
        }
        #endregion

        private void CopyAllText_Click(object sender, RoutedEventArgs e)
        {
            Main.CopyToClipboard(MainText.Text);
            Main.MousePopup("内容已复制到剪切板");
        }


        private void Translate_Click(object sender, RoutedEventArgs e)
        {
            string text = MainText.Text;
            if(!string.IsNullOrEmpty(MainText.SelectedText))
            {
                text = MainText.SelectedText;
            }
            string translateUri = $"https://translate.google.com/?hl=zh-cn&sl=auto&tl=zh-CN&text={text}&op=translate";
            webView.CoreWebView2.Navigate(translateUri);
            LogHelper.Log("用户进行了一次从文本处理框中翻译");
        }

        private void SearchSelectedText_Click(object sender, RoutedEventArgs e)
        {
            string text = MainText.Text;
            if (!string.IsNullOrEmpty(MainText.SelectedText))
            {
                text = MainText.SelectedText;
            }
            searchUri = Main.GetSESearchingUrl(text);
            webView.CoreWebView2.Navigate(searchUri);
            LogHelper.Log("用户进行了一次从文本处理框中搜索");
        }
        private void MainText_LostFocus(object sender, RoutedEventArgs e)
        {
            // 防止应失去焦点而取消选择文本
            e.Handled = true;
        }

        private void SaveScreen_Click(object sender, RoutedEventArgs e)
        {
            string filePath = ScreencatchHelper.GetScreenshotFilePath();
            screenImg.Save(filePath, ImageFormat.Png);
            Main.MousePopup($"截图已保存!", 2000);
            LogHelper.Log($"截图已保存至 {filePath}", LogLevel.Info);
        }

        private void HideBackground_Click(object sender, RoutedEventArgs e)
        {
            if (BackgroundImage.Opacity == BackgroundImgDefaultOpacity)
            {
                HideBackgroundImg(200);
                HideBackgroundIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.Show;
            }
            else
            {
                ShowBackgroundImg(200);
                HideBackgroundIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.Hide;
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            ShutdownWindow();
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            ShutdownWindow();
            Main.OpenConfigWindow();
            LogHelper.Log("用户从框定即搜窗口打开了设置窗口");
        }
        #region 控件获取焦点时取消框选
        private void ToolBox_GotFocus(object sender, RoutedEventArgs e)
        {
            ShutdownSelectRectangleMode(true);//免得在选中其他面板时还在框选 同时又不要取消框选
        }

        private void Browser_GotFocus(object sender, RoutedEventArgs e)
        {
            ShutdownSelectRectangleMode(true);//免得在选中其他面板时还在框选 同时又不要取消框选
        }

        private void TextProcess_GotFocus(object sender, RoutedEventArgs e)
        {
            ShutdownSelectRectangleMode(true);//免得在选中其他面板时还在框选 同时又不要取消框选
        }
        #endregion
    }
}
