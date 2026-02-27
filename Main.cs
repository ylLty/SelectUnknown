using MaterialDesignThemes.Wpf;
using SelectUnknown.ConfigManagment;
using SelectUnknown.HotKeyMan;
using SelectUnknown.Lens;
using SelectUnknown.LogManagement;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Policy;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Windows.Media.Protection.PlayReady;
using static System.Net.WebRequestMethods;
using Application = System.Windows.Application;
using Brushes = System.Windows.Media.Brushes;
using Clipboard = System.Windows.Forms.Clipboard;
using Color = System.Windows.Media.Color;
using File = System.IO.File;
using MessageBox = System.Windows.Forms.MessageBox;
using Point = System.Windows.Point;

namespace SelectUnknown
{
    class Main
    {
        #region App 信息
        public static string APP_NAME => GetAssemblyTitle();
        public static string APP_VERSION => GetInformationalVersion();

        public const int VERSION_CODE = 2;
        public static string COPYRIGHT_INFO => GetCopyright();
        public static string GetCopyright()
        {
            return Assembly.GetEntryAssembly()?
                .GetCustomAttribute<AssemblyCopyrightAttribute>()?
                .Copyright ?? string.Empty;
        }

        // 诊断方法 - 添加这些来查看所有版本信息
        public static void DebugVersionInfo()
        {
            var assembly = Assembly.GetEntryAssembly();

            Console.WriteLine("=== 版本信息诊断 ===");
            Console.WriteLine($"Assembly Version: {assembly?.GetName().Version}");
            Console.WriteLine($"FileVersion: {assembly?.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version}");
            Console.WriteLine($"InformationalVersion: {assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion}");

            // 检查文件版本信息
            var filePath = assembly?.Location;
            if (filePath != null && File.Exists(filePath))
            {
                var fileVersionInfo = FileVersionInfo.GetVersionInfo(filePath);
                Console.WriteLine($"FileVersionInfo.FileVersion: {fileVersionInfo.FileVersion}");
                Console.WriteLine($"FileVersionInfo.ProductVersion: {fileVersionInfo.ProductVersion}");
            }
        }

        private static string GetAssemblyTitle()
        {
            return Assembly.GetEntryAssembly()?
                .GetCustomAttribute<AssemblyTitleAttribute>()?
                .Title ?? Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly()?.Location) ?? "Recorder++";
        }

        public static string GetInformationalVersion()
        {
            var assembly = Assembly.GetEntryAssembly();
            return "V" + assembly?.GetName().Version.ToString() ?? new Version(1, 0, 0, 0).ToString();
        }
        #endregion
        #region 公共方法
        public static void OpenUrl(string url)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                LogHelper.Log($"打开链接失败: {url}，异常信息: {ex.Message}", LogLevel.Error);
            }
        }
        /// <summary>
        /// 在配置界面侧栏弹一条消息
        /// </summary>
        /// <param name="msg"></param>
        public static void PopupMessageOnConfigWindow(string msg)
        {
            // 获取当前活动的 ConfigWindow 实例
            foreach (Window window in System.Windows.Application.Current.Windows)
            {
                if (window is SelectUnknown.ConfigWindow configWindow)
                {
                    configWindow.PopupMsg(msg);
                    break;
                }
            }
        }
        private static readonly object _lock = new();
        private static int _inProgress = 0;
        /// <summary>
        /// 安全的复制文本到剪切板
        /// </summary>
        /// <param name="value"></param>
        public static void CopyToClipboard(string value)
        {
            if (string.IsNullOrEmpty(value))
                return;

            // 防止重入（非常关键）
            if (Interlocked.Exchange(ref _inProgress, 1) == 1)
                return;

            try
            {
                var dispatcher = System.Windows.Application.Current?.Dispatcher;
                if (dispatcher == null)
                    return;

                dispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    new Action(() =>
                    {
                        try
                        {
                            lock (_lock)
                            {
                                // 最安全的剪贴板写法
                                System.Windows.Clipboard.SetDataObject(value, false);
                            }
                        }
                        catch (COMException ex) when ((uint)ex.HResult == 0x800401D0)
                        {
                            // OpenClipboard 失败：忽略即可，绝不重试
                        }
                        catch
                        {
                            // 其他异常同样吞掉，避免污染 UI 线程
                        }
                    })
                );
            }
            finally
            {
                // 稍微延迟释放，避免极端连点
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    Thread.Sleep(30);
                    Interlocked.Exchange(ref _inProgress, 0);
                });
            }
        }
        public static bool IsPathValid(string path, bool allowEmpty = false)
        {
            // 1. 基本非空检查

            if (string.IsNullOrWhiteSpace(path))
            {
                if (allowEmpty)// 允许空路径(留空自动获取功能)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            try
            {
                // 2. 检查非法字符 (Windows 下通常包含 < > | " 等)
                // GetInvalidPathChars 在不同系统（Win/Linux）下返回不同结果
                if (path.Any(c => Path.GetInvalidPathChars().Contains(c)))
                {
                    return false;
                }

                // 3. 尝试获取完整路径
                // 虽然 .NET 8 GetFullPath 较宽松，但它能处理基本的格式错误
                string fullPath = Path.GetFullPath(path);

                // 4. 针对 Windows 的特殊检查：冒号 (:)
                // 在 Windows 中，冒号只能出现在盘符位置（如 C:\）
                // GetFullPath 可能会放过 "C:\abc:def" 这种路径
                int colonIndex = fullPath.IndexOf(':');
                if (colonIndex != -1)
                {
                    // 如果有冒号，它必须是第二个字符（如 C:），且后面跟着分隔符
                    // 或者是 UNC 路径（这里暂不深入 UNC 复杂性，仅处理常规盘符）
                    if (colonIndex != 1) return false;
                }

                // 5. 检查路径是否包含无效的文件/目录名
                // 比如 Windows 不允许文件名为 "CON", "PRN", "AUX" 等
                // 或者文件名包含 * 或 ? (GetInvalidPathChars 有时漏掉这些)
                string fileName = Path.GetFileName(fullPath);
                if (!string.IsNullOrEmpty(fileName))
                {
                    if (fileName.Any(c => Path.GetInvalidFileNameChars().Contains(c)))
                    {
                        return false;
                    }
                }

                // 6. 检查路径长度限制（可选）
                // Windows 默认限制是 260 字符，除非开启了长路径支持
                if (fullPath.Length >= 260) return false;

                return true;
            }
            catch (Exception)
            {
                // 捕获 ArgumentException, NotSupportedException 等
                return false;
            }
        }
        private static bool refused = false;
        public static async Task CheckUpdate(bool proactive = false)
        {
            if (refused && !proactive) return;

            var handler = new HttpClientHandler { UseProxy = false };

            HttpClient client = new HttpClient(handler);
            string url = "https://gitee.com/ylLty/ylLtyStaticRes/raw/main/SelectUnknown/update.json";// 老子不信这个还用不了
            
            // UA 标识
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (compatible; SelectUnknown/1.0)"
            ); 
            string json;
            try
            {
                // 发送 GET 请求并获取字符串内容
                json = await client.GetStringAsync(url);
            }
            catch (HttpRequestException ex)
            {
                LogHelper.Log($"检查更新失败：无法获取更新信息，异常信息: {ex}", LogLevel.Warn);
                MousePopup("检查更新失败，请检查网络");
                return;
            }

            if (string.IsNullOrEmpty(json))
            {
                LogHelper.Log("检查更新失败：未能获取更新信息", LogLevel.Warn);
                return;
            }
            AppUpdateInfo info;
            // 解析 JSON
            try
            {
                info = JsonSerializer.Deserialize<AppUpdateInfo>(json);
            }
            catch (JsonException ex)
            {
                LogHelper.Log($"检查更新失败：解析更新信息时发生错误，异常信息: {ex}", LogLevel.Warn);
                return;
            }
            if (info.VersionCode > VERSION_CODE)
            {
                LogHelper.Log($"检查更新结果：发现新版本 {info.Version} (VersionCode: {info.VersionCode})，更新内容: {info.ReleaseSummary}");
                DialogResult messageBoxButton = MessageBox.Show($"发现新版本 {info.Version}！\n\n更新内容：\n{info.ReleaseSummary}\n\n点击确定前往下载页面", "发现新版本！", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
                if (messageBoxButton == DialogResult.OK)
                {
                    OpenUrl(info.DownLoadUrl);
                    LogHelper.Log($"用户选择更新，已打开下载链接: {info.DownLoadUrl}");
                }
                else
                {
                    refused = true;
                }
                LogHelper.Log($"用户选择 {(messageBoxButton == DialogResult.OK ? "更新" : "取消")}");
            }
            else if(proactive)
            {
                MousePopup("已是最新版本");
            }
        }
        #region 鼠标消息弹窗
        private static Window _currentWindow;
        
        /// <summary>
        /// 鼠标消息
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="duration"></param>
        public static async void MousePopup(string msg, int duration = 3000)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // 不搞队列：有就直接关
                _currentWindow?.Close();
                _currentWindow = null;

                var text = new TextBlock
                {
                    Text = msg,
                    Foreground = Brushes.White,
                    FontSize = 13,
                    Margin = new Thickness(12, 6, 12, 6),
                    TextWrapping = TextWrapping.Wrap
                };

                var card = new Card
                {
                    Content = text,
                    Background = new SolidColorBrush(Color.FromRgb(33, 33, 33)),
                    Opacity = 0,
                    RenderTransform = new TranslateTransform(0, 6)
                };

                var win = new Window
                {
                    Content = card,
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = true,
                    Background = Brushes.Transparent,
                    ShowInTaskbar = false,
                    Topmost = true,
                    SizeToContent = SizeToContent.WidthAndHeight,
                    ShowActivated = false
                };

                // 鼠标位置
                var pos = GetMousePosition();
                win.Left = pos.X + 8;
                win.Top = pos.Y + 18;

                win.Show();

                _currentWindow = win;

                // 淡入 + 上移
                var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150));
                var moveUp = new DoubleAnimation(6, 0, TimeSpan.FromMilliseconds(150));

                card.BeginAnimation(UIElement.OpacityProperty, fadeIn);
                ((TranslateTransform)card.RenderTransform)
                    .BeginAnimation(TranslateTransform.YProperty, moveUp);
            });

            await Task.Delay(duration);

            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_currentWindow == null)
                    return;

                if (_currentWindow.Content is not Card card)
                    return;

                // 淡出 + 下移
                var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200));
                var moveDown = new DoubleAnimation(0, 6, TimeSpan.FromMilliseconds(200));

                fadeOut.Completed += (_, _) =>
                {
                    _currentWindow?.Close();
                    _currentWindow = null;
                };

                card.BeginAnimation(UIElement.OpacityProperty, fadeOut);
                ((TranslateTransform)card.RenderTransform)
                    .BeginAnimation(TranslateTransform.YProperty, moveDown);
            });
        }

        private static System.Drawing.Point GetMousePosition()
        {
            System.Drawing.Point mousePos = System.Windows.Forms.Cursor.Position;
            return mousePos;
            //return GetCursorScreenPosition();
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        //[DllImport("user32.dll")]
        //private static extern bool GetCursorPos(out POINT lpPoint);

        //public static Point GetCursorScreenPosition()
        //{
        //    GetCursorPos(out var p);
        //    return new Point(p.X, p.Y);
        //}
        #endregion
        /// <summary>
        /// 获取用户当前选择的文本
        /// </summary>
        /// <returns></returns>
        public static string GetSelectedText()
        {
            // 保存当前剪贴板内容，避免覆盖用户原有的数据
            var oldText = System.Windows.Forms.Clipboard.GetText();

            // 发送 Ctrl+C 快捷键 (需引用 System.Windows.Forms)
            System.Windows.Forms.SendKeys.SendWait("^c");

            // 给系统一点响应时间
            System.Threading.Thread.Sleep(100);

            string selectedText = System.Windows.Forms.Clipboard.GetText();

            // 内容相同表明没选择
            if (oldText == selectedText)
            {
                LogHelper.Log("用户没有选择文本");
                return "";
            }

            // 恢复原剪贴板内容
            try
            {
                System.Windows.Forms.Clipboard.SetText(oldText);
            }
            catch { }//静默捕获，免得用户提前复制了图片出问题
            
            LogHelper.Log("已提取用户选择的文本：**SECRET**");
            return selectedText;
        }
        /// <summary>
        /// 获取搜索引擎首页链接
        /// </summary>
        public static string GetSEHomeUrl()
        {
            switch (Config.curConfig.SearchEngineName)
            {
                case "Google":
                    return "https://www.google.com/";
                case "夸克":
                    return "https://ai.quark.cn/";// 安卓 UI 标识不可用，强制要求下载。
                case "Bing (海外版)":
                    return "https://www.bing.com/";//需要挂全局梯子，否则会重定向到必应中国（Edge 浏览器还会识别浏览器配置）
                case "必应":
                    return "https://cn.bing.com/";
                case "DuckDuckGo":
                    return "https://duckduckgo.com/";
                case "百度":
                    return "https://www.baidu.com/";
                case "Yandex":
                    return "https://www.yandex.com/";
                default:
                    MousePopup("无法识别的搜索引擎，已默认使用必应搜索引擎");
                    LogHelper.Log("无法识别的搜索引擎，已默认使用必应搜索引擎", LogLevel.Warn);
                    return "https://cn.bing.com/";//默认必应 毕竟是一个比较折中的搜索引擎(国内可访问, 体验较好, 虽然也开始收割用户了)
            }
        }
        /// <summary>
        /// 获取搜索链接
        /// </summary>
        /// <returns></returns>
        public static string GetSESearchingUrl(string searchingText)
        {
            searchingText = System.Net.WebUtility.UrlEncode(searchingText);// URL 编码，确保特殊字符不会破坏链接结构
            switch (Config.curConfig.SearchEngineName)
            {//{searchingText} 直接放浏览器里面搜索这个就行了
                case "Google":
                    return $"https://www.google.com/search?q={searchingText}";
                case "夸克":
                    return $"https://ai.quark.cn/s/?q={searchingText}";
                case "Bing (海外版)":
                    return $"https://www.bing.com/search?q={searchingText}";//需要挂全局梯子，否则会重定向到必应中国（Edge 浏览器还会识别浏览器配置）
                case "必应":
                    return $"https://cn.bing.com/search?q={searchingText}";
                case "DuckDuckGo":
                    return $"https://duckduckgo.com/?q={searchingText}";
                case "百度":
                    return $"https://www.baidu.com/s?wd={searchingText}";
                case "Yandex":
                    return $"https://yandex.com/search/?text={searchingText}";
                default:
                    MousePopup("无法识别的搜索引擎，已默认使用必应搜索引擎");
                    LogHelper.Log("无法识别的搜索引擎，已默认使用必应搜索引擎", LogLevel.Warn);
                    return $"https://cn.bing.com/search?q={searchingText}";//默认必应 毕竟是一个比较折中的搜索引擎(国内可访问, 体验较好, 虽然也开始收割用户了)
            }
        }
        public static string GetLensEngineUrl(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl) || imageUrl == "无")
            {
                switch (Config.curConfig.LensEngineName)// 上传失败处理
                {
                    case "Yandex":
                        return $"https://yandex.com/images/search?rpt=imageview&url=";
                    case "Google":
                        return $"https://google.com/";
                    case "百度":
                        return $"https://baidu.com/";
                    default:
                        MousePopup("无法识别的以图搜图引擎，已默认使用 Google 以图搜图引擎");
                        LogHelper.Log("无法识别的以图搜图引擎，已默认使用 Google 以图搜图引擎", LogLevel.Warn);
                        return $"https://lens.google.com/uploadbyurl?url={imageUrl}";//默认 Google
                }
            }
            switch (Config.curConfig.LensEngineName)
            {
                case "Yandex":
                    return $"https://yandex.com/images/search?rpt=imageview&url={imageUrl}";
                case "Google":
                    return $"https://lens.google.com/uploadbyurl?url={imageUrl}";
                case "百度":
                    return $"https://baidu.com/";
                default:
                    MousePopup("无法识别的以图搜图引擎，已默认使用 Google 以图搜图引擎");
                    LogHelper.Log("无法识别的以图搜图引擎，已默认使用 Google 以图搜图引擎", LogLevel.Warn);
                    return $"https://lens.google.com/uploadbyurl?url={imageUrl}";//默认 Google
            }
        }
        public static string GetTranslateEngineUrl(string text)
        {
            text = System.Net.WebUtility.UrlEncode(text);
            switch (Config.curConfig.TranslateEngineName)
            {
                case "Google":
                    return $"https://translate.google.com/?sl=auto&tl=zh-CN&text={text}&op=translate";
                case "DeepL":
                    return $"https://www.deepl.com/translator#en/zh/{text}";
                case "Bing":
                    return $"https://cn.bing.com/translator?ref=TThis&text={text}&from=auto&to=zh-Hans";
                case "搜狗":
                    return $"https://fanyi.sogou.com/text?keyword={text}&transfrom=auto&transto=zh-CHS";
                default:
                    MousePopup("无法识别的翻译引擎，已默认使用 Google Translate 翻译引擎");
                    LogHelper.Log("无法识别的翻译引擎，已默认使用 Google Translate 翻译引擎", LogLevel.Warn);
                    return $"https://translate.google.com/?sl=auto&tl=zh-CN&text={text}&op=translate";//默认 Google Translate
            }
        }
        internal static string GetWebViewUserAgent()// 现在只给了安卓 UA 自定义选项, 以后可能会加更多选项, 所以先留着这个方法占位
        {
            //Android Edge
            return "Mozilla/5.0 (Linux; Android 10; K) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/144.0.0.0 Mobile Safari/537.36 EdgA/144.0.0.0";
        }
        public static bool IsUrl(string url)
        {
            // 检查字符串是否不为空
            if (string.IsNullOrWhiteSpace(url)) return false;

            // 尝试解析为URI，UriKind.Absolute确保只有包含协议(如http)的链接被认定为真
            return Uri.TryCreate(url, UriKind.Absolute, out Uri uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }
        public static Bitmap CroppedBitmapToBmp(CroppedBitmap croppedBitmap)
        {
            if (croppedBitmap == null) return null;

            using (MemoryStream outStream = new MemoryStream())
            {
                // 使用 PNG 编码器保留透明度（如果需要）
                BitmapEncoder enc = new PngBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(croppedBitmap));
                enc.Save(outStream);

                // 从流中构造新的 Bitmap 对象
                return new Bitmap(outStream);
            }
        }
        public static void OpenConfigWindow()
        {
            var configWindow = System.Windows.Application.Current.MainWindow;
            configWindow.Show();
            if (configWindow.WindowState == WindowState.Minimized)
            {
                configWindow.WindowState = WindowState.Normal;
            }
            configWindow.Activate();
            configWindow.Focus();
        }
        
        #endregion

        #region 程序启动相关

        static Mutex? _mutex;
        private static bool isInitialized = false;
        /// <summary>
        /// 初始化整个应用
        /// </summary>
        internal static async void Init()
        {
            LogHelper.InitLog();
            LogHelper.Log("日志初始化成功!");
            if (isInitialized)
            {
                LogHelper.Log("发生异常: 应用已初始化", LogLevel.Error);
                throw new InvalidOperationException("应用已初始化");
            }
            isInitialized = true;

            LogHelper.InitExpectionHandler();
            LogHelper.Log("异常处理程序初始化成功!");

            ShowSessionInfo();

            // 防止软件重复启动
            bool createdNew;
            _mutex = new Mutex(true, "SelectUnknown_SingleInstance", out createdNew);
            if (!createdNew)
            {
                MessageBox.Show("Select Unknown 已运行，请在托盘中找到 Select Unknown 图标并继续操作", "软件已在运行", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                LogHelper.Log("软件已运行，取消启动", LogLevel.Warn);
                Environment.Exit(183);// Win32 错误代码：ERROR_ALREADY_EXISTS 这边顺便用一下
                return;
            }
            LogHelper.Log("软件未运行，正常启动");

            string resCheckResult = VerifyResources();
            if (resCheckResult != "NoException")
            {
                MessageBox.Show($"必要的资源文件缺失：\n{resCheckResult}\n程序无法启动，请重新安装软件", "资源文件缺失", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogHelper.Log("必要的资源文件缺失，程序终止启动", LogLevel.Error);
                Environment.Exit(2);
                return;
            }
            LogHelper.Log("即将初始化配置");
            ConfigManager.InitConfig();
            LogHelper.Log("配置加载成功!");

            LogHelper.Log($"{Config.curConfig.OldLogDeleteDays}天前的旧日志即将被清理");
            LogHelper.CleanOldLog(Config.curConfig.OldLogDeleteDays, LogHelper.logPath);

            TrayHelper.InitTray("Select Unknown", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "res", "logo.ico"));
            LogHelper.Log("托盘初始化成功!");
            HotKeyHelper.InitHotKey();
            LogHelper.Log("热键初始化成功!");

            try
            {
                await OCRHelper.InitOcr();
            }
            catch (Exception ex)
            {
                LogHelper.Log($"OCR 引擎初始化失败，异常信息: {ex}", LogLevel.Error);
                MessageBox.Show($"OCR 引擎初始化失败，异常信息: {ex.Message}\n请确保已正确安装 PaddleOCRJson 依赖，并尝试重新启动软件。若无法解决问题，请尝试切换 OCR 引擎", "OCR 初始化失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            
            LogHelper.Log("OCR 引擎初始化成功!");
        }
        private static void ShowSessionInfo()
        {
            LogHelper.Log("=== 程序运行信息 ===");

            // 1. 程序名称 & 版本
            var assembly = Assembly.GetExecutingAssembly();
            var name = assembly.GetName().Name;
            var version = assembly.GetName().Version?.ToString();
            LogHelper.Log($"程序名称: {name}");
            LogHelper.Log($"程序版本: {version}");

            // 2. 运行路径 (程序文件所在目录)
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            LogHelper.Log($"运行路径: {baseDirectory}");

            // 3. 日志路径 
            string logPath = LogHelper.GetLogPath();
            LogHelper.Log($"日志路径: {logPath}");

            // 4. 系统版本
            LogHelper.Log($"系统版本: {Environment.OSVersion} ({(Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit")})");

            // 5. 是否以管理员权限运行
            bool isAdmin = false;
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            LogHelper.Log($"管理员权限: {(isAdmin ? "是" : "否")}");

            LogHelper.Log("============================");
        }
        /// <summary>
        /// 检验必要的资源文件是否存在
        /// </summary>
        /// <returns></returns>
        private static string VerifyResources()
        {
            string resDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "res");
            if (!Directory.Exists(resDir))
            {
                LogHelper.Log($"资源文件夹缺失: {resDir}", LogLevel.Error);
                return $"资源文件夹缺失: {resDir}";
            }
            string[] requiredFiles = { "logo.ico", "load-window.png", "background.png" };
            foreach (var file in requiredFiles)
            {
                string filePath = Path.Combine(resDir, file);
                if (!File.Exists(filePath))
                {
                    LogHelper.Log($"缺失必要的资源文件: {filePath}", LogLevel.Error);
                    return $"资源文件缺失: {filePath}";
                }
            }
            LogHelper.Log("所有必要的资源文件均已存在");
            return "NoException";
        }

        
        #endregion
    }
    public class AppUpdateInfo
    {
        public string Version { get; set; }
        public int VersionCode { get; set; }
        public string ReleaseSummary { get; set; }
        public string DownLoadUrl { get; set; }
    }
}
