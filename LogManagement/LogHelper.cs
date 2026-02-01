using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.CompilerServices;

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
            if (!Directory.Exists(logPath))
            {
                Directory.CreateDirectory(logPath);
            }
            System.Diagnostics.Process.Start("explorer.exe", logPath);
        }
    }
}
