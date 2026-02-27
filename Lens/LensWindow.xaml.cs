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

            //Clipboard.SetDataObject(bmps);
            
            screenImg = scrImg;

            isAutoSele = true;
            AutoSele.Background = selectingBrush;
            AutoSele.BorderBrush = selectingBrush;
            AutoSele.Foreground = selectingForeBrush;
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
            ScreenImage.MouseLeftButtonDown -= StartSelectRectangle;
            ScreenImage.MouseMove -= MovingSelectRectangle;
            ScreenImage.MouseLeftButtonUp -= EndSelectRectangle;
            SelectionCanvas.MouseLeftButtonDown -= StartSelectRectangle;
            SelectionCanvas.MouseMove -= MovingSelectRectangle;
            SelectionCanvas.MouseLeftButtonUp -= EndSelectRectangle;
            ScreenImage.MouseRightButtonUp -= ScreenImage_MouseRightButtonUp;
            SelectionCanvas.MouseRightButtonUp -= SelectionCanvas_MouseRightButtonUp;
            AutoSele.Click -= AutoSele_Click;
            TextOnly.Click -= TextOnly_Click;
            ImgOnly.Click -= ImgOnly_Click;
            TranslateOnly.Click -= TranslateOnly_Click;
            OcrOnly.Click -= OcrOnly_Click;
            CopyAllText.Click -= CopyAllText_Click;
            Translate.Click -= Translate_Click;
            SearchSelectedText.Click -= SearchSelectedText_Click;
            MainText.LostFocus -= MainText_LostFocus;
            SaveScreen.Click -= SaveScreen_Click;
            HideBackground.Click -= HideBackground_Click;
            Close.Click -= Close_Click;
            Settings.Click -= Settings_Click;
            SaveImg.Click -= SaveImg_Click;
            BrowserDrag.MouseLeftButtonDown -= Border_MouseLeftButtonDown;
            BrowserDrag.MouseLeftButtonUp -= Border_MouseLeftButtonUp;
            BrowserDrag.MouseMove -= Border_MouseMove;
            BrowserDrag.MouseDown -= Border_MouseDown;
            BrowserDrag.MouseMove -= Border_MouseMove_1;
            BrowserDrag.MouseUp -= Border_MouseUp;
            BrowserDrag.MouseDown -= Border_MouseDown_1;
            BrowserDrag.MouseMove -= Border_MouseMove_2;
            BrowserDrag.MouseUp -= Border_MouseUp_1;

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
            if (Config.curConfig.UsingAndroidUserAgent)
            {
                webView.CoreWebView2.Settings.UserAgent = Main.GetWebViewUserAgent();//设置为安卓 UA
                LogHelper.Log("已设置 WebView2 使用安卓用户代理");
            }
            SelectRectangle_Click(sender, e);
            if (Config.curConfig.AutoCheckUpdate)
                Task.Run(() => Main.CheckUpdate(false));
            try
            {
                ScanQRCode(screenImg);
            }
            catch { }// 免得刚打开就关闭而出异常
        }
        #region 二维码识别
        private async void ScanQRCode(Bitmap bitmap)
        {
            if (closed) return;// 免得刚打开就关闭而出异常
            await Task.Run(() =>
            {
                try
                { 
                    bitmap = OCRHelper.ToGrayscaleBitmap(bitmap);
                }
                catch
                {
                    return;
                }
            });//借用一下 OCR 那边的灰度化
            if (closed) return;
            var results = await Task.Run(() => QrcodeHelper.Decode(bitmap));
            if (closed) return; 
            if (results.Count == 0)
            {
                //Main.MousePopup("未识别二维码");
                return;
            }
            if (closed) return;
            DrawCodeCenters(results, bitmap);
            //foreach (var r in results)
            //{
            //    MainText.Text += "\n" + ($"{r.Format} | {r.Text} | Center=({r.Center.X},{r.Center.Y})");
            //}
        }
        private void DrawCodeCenters(IReadOnlyList<CodeResult> codes, Bitmap bitmap)
        {
            CodeOverlay.Children.Clear();
            CodeOverlay.Background = null; // 空白透传，点击穿透到 Image
            CodeOverlay.IsHitTestVisible = true;

            const double size = 26;

            foreach (var code in codes)
            {
                System.Windows.Point pt = GetDisplayPoint(bitmap, code.Center, ScreenImage);

                var border = new Border
                {
                    Width = size,
                    Height = size,
                    CornerRadius = new CornerRadius(6),
                    Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(160, 30, 255, 30)),
                    BorderBrush = System.Windows.Media.Brushes.Lime,
                    BorderThickness = new Thickness(2),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Tag = code,
                    IsHitTestVisible = true
                };

                border.MouseLeftButtonDown += OnCodeLeftClick;
                border.MouseRightButtonDown += OnCodeRightClick;

                Canvas.SetLeft(border, pt.X - size / 2);
                Canvas.SetTop(border, pt.Y - size / 2);

                CodeOverlay.Children.Add(border);
            }
        }
        /// <summary>
        /// 将 Bitmap 中的像素坐标映射到 Image 控件显示坐标
        /// 适用于 Stretch=UniformToFill
        /// </summary>
        /// <summary>
        /// 将 Bitmap 像素坐标转换为 Image 上的显示坐标（考虑 Stretch=Uniform 留白）
        /// </summary>
        private static System.Windows.Point GetDisplayPoint(Bitmap bitmap, PointF center, System.Windows.Controls.Image image)
        {
            double imgWidth = image.ActualWidth;
            double imgHeight = image.ActualHeight;

            double bmpWidth = bitmap.Width;
            double bmpHeight = bitmap.Height;

            // Uniform 缩放比例
            double scale = Math.Min(imgWidth / bmpWidth, imgHeight / bmpHeight);

            // 计算偏移（空白留白）
            double offsetX = (imgWidth - bmpWidth * scale) / 2.0;
            double offsetY = (imgHeight - bmpHeight * scale) / 2.0;

            double x = center.X * scale + offsetX;
            double y = center.Y * scale + offsetY;

            return new System.Windows.Point(x, y);
        }




        private void OnCodeLeftClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Border border)
                return;

            if (border.Tag is not CodeResult code)
                return;

            if (Main.IsUrl(code.Text))
            {
                Main.OpenUrl(code.Text);
                ShutdownWindow();
            }
            else
            {
                Main.MousePopup("二维码内容不是链接，已执行解析操作");
                MainText.Text = code.Text;
            }
            LogHelper.Log("用户左键点击了二维码中心点");

            e.Handled = true;
        }
        private void OnCodeRightClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Border border)
                return;

            if (border.Tag is not CodeResult code)
                return;

            MainText.Text = code.Text;
            Main.MousePopup("二维码内容已显示在文本处理框中");
            LogHelper.Log("用户右键点击了二维码中心点，二维码内容已显示在文本处理框中");
            e.Handled = true;
        }

        #endregion
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
        private async void webView_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            if (webView == null || webView.CoreWebView2 == null) return;
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
            if (croppedImg.Height <= 1 || croppedImg.Width <= 1) return;

            async Task<string> RecTextAsync(bool silent = false)
            {
                string txt = null;
                if (croppedImg.Height >= 500 && croppedImg.Width >= 500)
                {
                    if (!silent)
                        Main.MousePopup("区域过大，无法识别");
                    return null;
                }

                TextProcessLoading.Visibility = Visibility.Visible;
                try
                {
                    txt = await Task.Run(() => OCRHelper.RecognizeAsync(croppedImg));

                    TextProcessLoading.Visibility = Visibility.Hidden;
                }
                catch (Exception ex)
                {
                    LogHelper.Log($"文字识别时出错: {ex}", LogLevel.Error);
                    Main.MousePopup("文字识别出错，请重试");
                    return null;
                }
                return txt;
        
            }

            string croppedTxt = null;
            string mode = GetMode();
        ReSelectMode:
            switch (mode)
            {
                case "AutoSele":
                    croppedTxt = await RecTextAsync(true);
                    mode = GetAutoMode(croppedTxt);
                    goto ReSelectMode;// 得到结果后重新选择模式（goto 小用不算用，写别的更麻烦）
                case "TextOnly":
                    croppedTxt = await RecTextAsync();
                    if (!string.IsNullOrEmpty(croppedTxt))
                    {
                        MainText.Text = croppedTxt;
                        //Main.MousePopup("文字识别完成，结果已显示在文本处理面板");
                        LogHelper.Log("文字识别完成，结果已显示在文本处理面板");
                    }
                    else
                    {
                        Main.MousePopup("未能识别出文字");
                        LogHelper.Log("未能识别出文字", LogLevel.Warn);
                    }
                    string scUrl = Main.GetSESearchingUrl(croppedTxt);
                    webView.CoreWebView2.Navigate(scUrl);
                    LogHelper.Log("用户进行了一次从框选区域中搜索文字");
                    break;
                case "ImgOnly":
                    ImageToLens(croppedImg);
                    break;
                case "TranslateOnly":
                    croppedTxt = await RecTextAsync();
                    if (!string.IsNullOrEmpty(croppedTxt))
                    {
                        MainText.Text = croppedTxt;
                        //Main.MousePopup("文字识别完成，结果已显示在文本处理面板");
                        LogHelper.Log("文字识别完成，结果已显示在文本处理面板");
                    }
                    else
                    {
                        Main.MousePopup("未能识别出文字");
                        LogHelper.Log("未能识别出文字", LogLevel.Warn);
                    }
                    string tsltUrl = Main.GetTranslateEngineUrl(croppedTxt);
                    webView.CoreWebView2.Navigate(tsltUrl);
                    LogHelper.Log("用户进行了一次从框选区域中翻译文字");
                    break;
                case "OcrOnly":
                    croppedTxt = await RecTextAsync();
                    if (!string.IsNullOrEmpty(croppedTxt))
                    {
                        MainText.Text = croppedTxt;
                        //Main.MousePopup("文字识别完成，结果已显示在文本处理面板");
                        LogHelper.Log("文字识别完成，结果已显示在文本处理面板");
                    }
                    else
                    {
                        Main.MousePopup("未能识别出文字");
                        LogHelper.Log("未能识别出文字", LogLevel.Warn);
                    }
                    break;
                case "None":
                    Clipboard.SetImage(croppedImg);
                    LogHelper.Log("不进行操作");
                    break;
                default:
                    Main.MousePopup("错误：未知的处理模式");
                    LogHelper.Log($"未知的处理模式: {mode}", LogLevel.Error);
                    break;
            }
            
        }
        static DateTime failTime;
        /// <summary>
        /// 分析图片
        /// </summary>
        /// <param name="bmp"></param>
        /// <returns></returns>
        public async void ImageToLens(Bitmap croppedImg)
        {
            Loading.Visibility = Visibility.Visible;
            string imageUrl = "无";
            if (!(Config.curConfig.LensEngineName == "百度" || Config.curConfig.LensEngineName == "Bing"))
            {
                if( (DateTime.Now - failTime).TotalSeconds >= 40)// 40s 冷却
                {
                    imageUrl = await LitterboxUploader.SendImageToLitterboxAndGetUrl(croppedImg);
                }
                else
                {
                    Main.MousePopup("图片上传失败，请检测网络，或尝试手动粘贴（已复制）", 2000);
                }
            }
            else 
            { 
                Main.MousePopup("当前引擎暂不支持图片快捷上传，烦请手动粘贴（已复制）", 2000);
                Clipboard.SetImage(croppedImg);
            }

            if (string.IsNullOrEmpty(imageUrl))
            {
                failTime = DateTime.Now;
                Main.MousePopup("图片上传失败，请检测网络，或尝试手动粘贴（已复制）");
                Clipboard.SetImage(croppedImg);
                Loading.Visibility = Visibility.Collapsed;
                LogHelper.Log("图片上传至 Litterbox 失败", LogLevel.Error);
            }
            if (webView == null || webView.CoreWebView2 == null) return;
            currentLensUrl = Main.GetLensEngineUrl(imageUrl);
            webView.CoreWebView2.Navigate(currentLensUrl);
            isLensSearching = true;
            lensTimes = navigationTimes;
            LogHelper.Log($"用户完成了一次框选并上传至 {Config.curConfig.LensEngineName} 进行分析");
            
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
            string translateUri = Main.GetTranslateEngineUrl(text);
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
            Clipboard.SetImage(screenImg);
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
        

        private void SaveImg_Click(object sender, RoutedEventArgs e)
        {
            Bitmap bitmap = GetSelectedImg();
            Clipboard.SetImage(bitmap);
            string filePath = ScreencatchHelper.GetScreenshotFilePath();
            try
            {
                bitmap.Save(filePath, ImageFormat.Png);
                Main.MousePopup($"图片已保存!", 2000);
                LogHelper.Log($"图片已保存至 {filePath}", LogLevel.Info);
            }
            catch (Exception ex)
            {
                LogHelper.Log($"保存图片时出错: {ex}", LogLevel.Error);
                Main.MousePopup("保存图片时出错，请重试");
            }
        }
        #region 模式选择
        static BrushConverter brushConverter = new BrushConverter();
        static System.Windows.Media.Brush defaultBrush = (System.Windows.Media.Brush)brushConverter.ConvertFromString("#00bcd4");
        static System.Windows.Media.Brush selectingBrush = (System.Windows.Media.Brush)brushConverter.ConvertFromString("#0b57cf");
        static System.Windows.Media.Brush foreBrush = (System.Windows.Media.Brush)brushConverter.ConvertFromString("#FF292929");
        static System.Windows.Media.Brush selectingForeBrush = (System.Windows.Media.Brush)brushConverter.ConvertFromString("#ffffff");
        public static bool isAutoSele { get; private set; } = true;
        public static bool isTextOnly { get; private set; } = false;
        public static bool isImgOnly { get; private set; } = false;
        public static bool isTranslateOnly { get; private set; } = false;
        public static bool isOcrOnly { get; private set; } = false;
        private void AutoSele_Click(object sender, RoutedEventArgs e)
        {
            if (isAutoSele)
            {
                isAutoSele = false;
                AutoSele.Background = defaultBrush;
                AutoSele.BorderBrush = defaultBrush;
                AutoSele.Foreground = foreBrush;
            }
            else
            {
                isAutoSele = true;
                AutoSele.Background = selectingBrush;
                AutoSele.BorderBrush = selectingBrush;
                AutoSele.Foreground = selectingForeBrush;
            }
            ShutdownOtherModes("AutoSele");
        }

        private void TextOnly_Click(object sender, RoutedEventArgs e)
        {
            if (isTextOnly)
            {
                isTextOnly = false;
                TextOnly.Background = defaultBrush;
                TextOnly.BorderBrush = defaultBrush;
                TextOnly.Foreground = foreBrush;
            }
            else
            {
                isTextOnly = true;
                TextOnly.Background = selectingBrush;
                TextOnly.BorderBrush = selectingBrush;
                TextOnly.Foreground = selectingForeBrush;
            }
            ShutdownOtherModes("TextOnly");
        }

        private void ImgOnly_Click(object sender, RoutedEventArgs e)
        {
            if (isImgOnly)
            {
                isImgOnly = false;
                ImgOnly.Background = defaultBrush;
                ImgOnly.BorderBrush = defaultBrush;
                ImgOnly.Foreground = foreBrush;
            }
            else
            {
                isImgOnly = true;
                ImgOnly.Background = selectingBrush;
                ImgOnly.BorderBrush = selectingBrush;
                ImgOnly.Foreground = selectingForeBrush;
            }
            ShutdownOtherModes("ImgOnly");
        }

        private void TranslateOnly_Click(object sender, RoutedEventArgs e)
        {
            if (isTranslateOnly)
            {
                isTranslateOnly = false;
                TranslateOnly.Background = defaultBrush;
                TranslateOnly.BorderBrush = defaultBrush;
                TranslateOnly.Foreground = foreBrush;
            }
            else
            {
                isTranslateOnly = true;
                TranslateOnly.Background = selectingBrush;
                TranslateOnly.BorderBrush = selectingBrush;
                TranslateOnly.Foreground = selectingForeBrush;
            }
            ShutdownOtherModes("TranslateOnly");
        }

        private void OcrOnly_Click(object sender, RoutedEventArgs e)
        {
            if (isOcrOnly)
            {
                isOcrOnly = false;
                OcrOnly.Background = defaultBrush;
                OcrOnly.BorderBrush = defaultBrush;
                OcrOnly.Foreground = foreBrush;
            }
            else
            {
                isOcrOnly = true;
                OcrOnly.Background = selectingBrush;
                OcrOnly.BorderBrush = selectingBrush;
                OcrOnly.Foreground = selectingForeBrush;
            }
            ShutdownOtherModes("OcrOnly");
        }
        bool exeing = false;
        private void ShutdownOtherModes(string except)
        {
            if (exeing) return;//防止递归调用
            exeing = true;
            //return;
            if (except != "AutoSele" && isAutoSele) AutoSele_Click(null, null);
            if (except != "TextOnly" && isTextOnly) TextOnly_Click(null, null);
            if (except != "ImgOnly" && isImgOnly) ImgOnly_Click(null, null);
            if (except != "TranslateOnly" && isTranslateOnly) TranslateOnly_Click(null, null);
            if (except != "OcrOnly" && isOcrOnly) OcrOnly_Click(null, null);
            Task.Run(() =>
            {
                Thread.Sleep(100);
                exeing = false;
            });
        }

        private string GetMode()
        {
            if (isAutoSele) return "AutoSele";
            if (isTextOnly) return "TextOnly";
            if (isImgOnly) return "ImgOnly";
            if (isTranslateOnly) return "TranslateOnly";
            if (isOcrOnly) return "OcrOnly";
            return "None";
        }
        /// <summary>
        /// 智能判断模式，根据用户选择的区域自动判断是进行文本处理还是图片分析，并返回相应的结果
        /// </summary>
        /// <returns></returns>
        private string GetAutoMode(string croppedTxt)
        {
            Bitmap selectedImg = GetSelectedImg();
            if (selectedImg == null) return "TextOnly";
            // 正式开始判断
            if (string.IsNullOrWhiteSpace(croppedTxt)) return "ImgOnly";// 没有识别出文字返回图片识别

            if (selectedImg.Height >= 70) return "ImgOnly"; //太高了，认为是框图片

            if (IsEnglishStringASentence(croppedTxt)) return "TranslateOnly";//全是英文且较长（可能是句子）我认为是在翻译
            
            return "TextOnly";//默认搜索文字
        }
        private bool IsEnglishStringASentence(string str)
        {
            if (HasNonAscii(str)) return false; // 有中文返回不是
            if (GetWordsCount(str) >= 4) return true;
            return false;
        }

        /// <summary>
        /// 检测字符串有无汉字（或其他非 ASCII 字符）有汉字true
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private bool HasNonAscii(string str)
        {
            foreach (char c in str)
            {
                if (c > 127) return true; // 发现一个非ASCII立即返回
            }
            return false;
        }
        private int GetWordsCount(string sentence)
        {
            string[] words = sentence.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            int wordCount = words.Length;
            return wordCount;
        }
        #endregion

        private void ScreenImage_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            ShutdownSelectRectangleMode();
        }

        private void SelectionCanvas_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            ShutdownSelectRectangleMode();
        }
    }
}
