using SelectUnknown.ConfigManagment;
using SelectUnknown.HotKeyMan;
using SelectUnknown.LogManagement;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using MessageBox = System.Windows.Forms.MessageBox;

namespace SelectUnknown
{
    class Main
    {
        #region App 信息
        public static string APP_NAME => GetAssemblyTitle();
        public static string APP_VERSION => GetInformationalVersion();
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
        #endregion
        static Mutex? _mutex;
        private static bool isInitialized = false;
        /// <summary>
        /// 初始化整个应用
        /// </summary>
        internal static void Init()
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

            // 防止软件重复启动
            bool createdNew;
            _mutex = new Mutex(true, "SelectUnknown_SingleInstance", out createdNew);
            if (!createdNew)
            {
                MessageBox.Show("Select Unknown 已运行，请在托盘中找到 Select Unknown 图标并继续操作", "软件已在运行", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                LogHelper.Log("软件已运行，取消启动", LogLevel.Warn);
                System.Windows.Application application = System.Windows.Application.Current;
                application.Shutdown(-403);
                return;
            }
            LogHelper.Log("软件未运行，正常启动");

            string resCheckResult = VerifyResources();
            if (resCheckResult != "NoException")
            {
                MessageBox.Show($"必要的资源文件缺失：\n{resCheckResult}\n程序无法启动，请重新安装软件", "资源文件缺失", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogHelper.Log("必要的资源文件缺失，程序终止启动", LogLevel.Error);
                System.Windows.Application application = System.Windows.Application.Current;
                application.Shutdown(-404);
                return;
            }
            LogHelper.Log("即将初始化配置");
            ConfigManager.InitConfig();
            LogHelper.Log("配置加载成功!");

            LogHelper.Log($"{Config.OldLogDeleteDays}天前的旧日志即将被清理");
            LogHelper.CleanOldLog(Config.OldLogDeleteDays, LogHelper.logPath);

            TrayHelper.InitTray("Select Unknown", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "res", "logo.ico"));
            LogHelper.Log("托盘初始化成功!");
            HotKeyHelper.InitHotKey();
            LogHelper.Log("热键初始化成功!");
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
            string[] requiredFiles = { "logo.ico", "load-window.png" };
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
    }
}
