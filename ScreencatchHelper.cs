using SelectUnknown.ConfigManagment;
using SelectUnknown.LogManagement;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static System.Windows.Forms.DataFormats;
using Point = System.Drawing.Point;

namespace SelectUnknown
{
    class ScreencatchHelper
    {
        public static void Screenshot()
        {
            Bitmap bmp = CaptureScreen();
            string filePath = GetScreenshotFilePath();
            bmp.Save(filePath, ImageFormat.Png);
            LogHelper.Log($"截图已保存至 {filePath}", LogLevel.Info);
        }
        public static string GetScreenshotFolderPath()
        {
            string directory;
            if (string.IsNullOrWhiteSpace(Config.curConfig.ScreenshotFolderPath))
            {
                // 留了空，自动获取图片文件夹路径
                directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "SelectUnknownScreenshots");
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                return directory;
            }
            else
            {
                if (!Main.IsPathValid(Config.curConfig.ScreenshotFolderPath))
                {
                    System.Windows.MessageBox.Show("截图保存路径无效，请重新配置\n（图片已暂存至日志文件夹，请在 关于软件 - 查看日志 找到图片文件）", Main.APP_NAME, MessageBoxButton.OK, MessageBoxImage.Warning);
                    LogHelper.Log($"截图文件夹路径无效，需重新配置({Config.curConfig.ScreenshotFolderPath})图片已暂存至日志文件夹", LogLevel.Warn);
                    return LogHelper.GetLogPath();
                }
                return Path.GetFullPath(Config.curConfig.ScreenshotFolderPath);
            }
        }
        public static string GetScreenshotFilePath()
        {
            string directory = GetScreenshotFolderPath();
            string filePath = Path.Combine(directory, $"Screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png");
            return filePath;
        }
        private const PixelFormat FORMAT = PixelFormat.Format24bppRgb;
        public static Bitmap CaptureScreen()
        {
            Bitmap screenshot = new Bitmap(
        Screen.PrimaryScreen.Bounds.Width,
        Screen.PrimaryScreen.Bounds.Height,
        FORMAT
    );
            using (Graphics gfx = Graphics.FromImage(screenshot))
            {
                gfx.CopyFromScreen(
                    Screen.PrimaryScreen.Bounds.X,
                    Screen.PrimaryScreen.Bounds.Y,
                    0,
                    0,
                    Screen.PrimaryScreen.Bounds.Size,
                    CopyPixelOperation.SourceCopy
                );
                return screenshot;
            }
        }
    }
}
