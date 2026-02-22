using GlobalHotKey;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SelectUnknown.ConfigManagment
{
    public static class Config
    {
        #region 常规设置
        public static bool SilentStart { get; set; } = false;
        public static int OldLogDeleteDays { get; set; } = 2;
        public static string ScreenshotFolderPath { get; set; } = "";
        #endregion
        #region 热键设置
        public static Key StartKey { get; set; } = Key.O;
        public static ModifierKeys[] StartModifierKeys { get; set; } = { ModifierKeys.Control };
        public static Key ScreenshotKey { get; set; } = Key.I;
        public static ModifierKeys[] ScreenshotModifierKeys { get; set; } = { ModifierKeys.Control, ModifierKeys.Alt };
        #endregion
        #region 服务设置
        public static string SearchEngineName { get; set; } = "Google";
        public static bool UsingAndroidUserAgent { get; set; } = true;
        public static string LensEngineName { get; set; } = "Google";
        public static string TranslateEngineName { get; set; } = "Google";
        public static string OcrEngineName { get; set; } = "Windows 内置";
        #endregion
    }
}
