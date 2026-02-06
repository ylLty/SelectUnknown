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
            if (string.IsNullOrWhiteSpace(Config.ScreenshotFolderPath))
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
                if (!Main.IsPathValid(Config.ScreenshotFolderPath))
                {
                    System.Windows.MessageBox.Show("截图保存路径无效，请重新配置\n（图片已暂存至日志文件夹，请在 关于软件 - 查看日志 找到图片文件）", Main.APP_NAME, MessageBoxButton.OK, MessageBoxImage.Warning);
                    LogHelper.Log($"截图文件夹路径无效，需重新配置({Config.ScreenshotFolderPath})图片已暂存至日志文件夹", LogLevel.Warn);
                    return LogHelper.GetLogPath();
                }
                return Path.GetFullPath(Config.ScreenshotFolderPath);
            }
        }
        public static string GetScreenshotFilePath()
        {
            string directory = GetScreenshotFolderPath();
            string filePath = Path.Combine(directory, $"Screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png");
            return filePath;
        }
        static Bitmap CaptureScreen()
        {
            int width = (int)SystemParameters.VirtualScreenWidth;
            int height = (int)SystemParameters.VirtualScreenHeight;
            int left = (int)SystemParameters.VirtualScreenLeft;
            int top = (int)SystemParameters.VirtualScreenTop;

            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);

            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(left, top, 0, 0, bmp.Size, CopyPixelOperation.SourceCopy);
            }

            return bmp;
        }
    }
}
