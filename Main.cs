using SelectUnknown.LogManagement;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
        #endregion
        static Mutex? _mutex;
        /// <summary>
        /// 初始化整个应用
        /// </summary>
        internal static void Init()
        {
            LogHelper.InitLog();
            LogHelper.Log("日志初始化成功!");
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

            TrayHelper.InitTray("Select Unknown", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "res", "logo.ico"));
            LogHelper.Log("托盘初始化成功!");
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
