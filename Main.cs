using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using SelectUnknown.LogManagement;

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
        /// <summary>
        /// 初始化整个应用
        /// </summary>
        internal static void Init()
        {
            LogHelper.InitLog();
            LogHelper.Log("日志初始化成功!");
            TrayHelper.InitTray("Select Unknown", "I:\\Five_ID_Num\\Five_ID_Num\\icon\\Five_ID_Num-32.png.ico");
        }
    }
}
