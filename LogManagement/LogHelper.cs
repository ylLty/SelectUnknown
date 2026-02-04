using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;

namespace SelectUnknown.LogManagement
{
    class LogHelper
    {
        /// <summary>
        /// 日志文件夹路径
        /// </summary>
        public static string logPath = GetLogPath();
        public static string logFilePath = GetLogFilePath();
        /// <summary>
        /// 初始化日志功能
        /// </summary>
        public static void InitLog()
        {
            logPath = GetLogPath();
            if (!Directory.Exists(logPath))
            {
                Directory.CreateDirectory(logPath);
            }
            logFilePath = GetLogFilePath();
        }
        /// <summary>
        /// 记录一条日志
        /// </summary>
        /// <param name="info"></param>
        /// <param name="level"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Log(string info, LogLevel level = LogLevel.Info)
        {
            string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss:fff}] [{level}] {info}";
            
            // 如果文件不存在，创建文件
            if (!File.Exists(logFilePath))
            {
                File.Create(logFilePath).Dispose();
            }
            using (StreamWriter writer = new StreamWriter(logFilePath, true))
            {
                writer.WriteLine(logMessage);
            }
            Console.WriteLine(logMessage);
        }
        /// <summary>
        /// 获取日志文件路径
        /// </summary>
        private static string GetLogPath()
        {
            return Path.Combine(Path.GetTempPath(), "SelectUnknown", "logs");
        }
        private static string GetLogFilePath()
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            return Path.Combine(logPath, $"{timestamp}.log");
        }
        /// <summary>
        /// 打开日志文件夹
        /// </summary>
        public static void OpenLogDirectory()
        {
            Process.Start("explorer.exe", $"/select,\"{LogHelper.logFilePath}\"");
        }
        public static void InitExpectionHandler()
        {
            // WPF UI线程异常
            System.Windows.Application.Current.DispatcherUnhandledException += (sender, e) =>
            {
                HandleException(e.Exception, "DispatcherUnhandledException");
                e.Handled = true; // 阻止崩溃
            };

            // 应用程序域异常
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                var exception = e.ExceptionObject as Exception;
                HandleException(exception, "AppDomain UnhandledException");
            };

            // Task异常
            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                HandleException(e.Exception, "UnobservedTaskException");
                e.SetObserved();
            };
        }

        private static void HandleException(Exception ex, string source)
        {
            var logEntry = new
            {
                Timestamp = DateTime.UtcNow,
                Source = source,
                ExceptionType = ex.GetType().Name,
                Message = ex.Message,
                StackTrace = ex.StackTrace,
                InnerException = ex.InnerException?.Message
            };
            

            // 在UI线程显示错误
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                // 手动替换常见的转义字符
                string formatted = JsonSerializer.Serialize(logEntry, JsonSerializerOptions.Default)
                    .Replace("\\\"", "\"")      // 双引号
                    .Replace("\\\\", "\\")      // 反斜杠
                    .Replace("\\n", "\n")       // 换行
                    .Replace("\\r", "\r")       // 回车
                    .Replace("\\t", "\t")       // 制表符
                    .Replace("\\b", "\b")       // 退格
                    .Replace("\\f", "\f")       // 换页
                    .Replace("\\/", "/");       // 斜杠

                // 异步记录日志，避免阻塞UI
                Task.Run(() => Log("致命错误！\n" + formatted + "\n来源：" + source, LogLevel.Error));

                System.Windows.MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show(
                    "Select Unknown 软件运行时发生了未经处理的异常，继续运行可能会出现问题\n按下确定打开日志文件，请将日志文件提交给作者\n按下取消继续运行\n" +
                    formatted,
                    "致命错误", 
                    System.Windows.MessageBoxButton.OKCancel, 
                    System.Windows.MessageBoxImage.Error
                    );
                if(messageBoxResult == System.Windows.MessageBoxResult.OK)
                {
                    OpenLogDirectory();
                    Environment.Exit(1); // 退出应用程序
                }
            });
        }
        /// <summary>
        /// 清理旧的日志
        /// </summary>
        /// <param name="day"></param>
        /// <param name="logDir"></param>
        public static void CleanOldLog(int day, string logDir)
        {
            // 检查目录是否存在
            if (!Directory.Exists(logDir))
            {
                Log("清理日志时发现指定的日志目录不存在！", LogLevel.Warn);
                return;
            }

            // 获取所有 .log 文件
            string[] logFiles = Directory.GetFiles(logDir, "*.log");

            // 当前日期
            DateTime currentDate = DateTime.Now;

            foreach (string logFile in logFiles)
            {
                // 获取文件的最后修改时间
                DateTime lastWriteTime = File.GetLastWriteTime(logFile);

                // 如果文件修改日期超过指定天数，则删除
                if ((currentDate - lastWriteTime).Days > day)
                {
                    try
                    {
                        File.Delete(logFile);
                        Log($"已删除过期日志文件: {logFile}");
                    }
                    catch (Exception ex)
                    {
                        Log($"清理日志文件失败: {logFile}, 错误: {ex.Message}", LogLevel.Warn);
                    }
                }
            }
        }
    }
}
