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
    public class Config
    {
        public static Config curConfig = new Config();
        #region 常规设置
        public bool SilentStart { get; set; } = false;
        public bool AutoCheckUpdate { get; set; } = true;
        public int OldLogDeleteDays { get; set; } = 2;
        public string ScreenshotFolderPath { get; set; } = "";
        #endregion
        #region 热键设置
        public Key StartKey { get; set; } = Key.O;
        public ModifierKeys[] StartModifierKeys { get; set; } = { ModifierKeys.Control };
        public Key ScreenshotKey { get; set; } = Key.I;
        public ModifierKeys[] ScreenshotModifierKeys { get; set; } = { ModifierKeys.Control, ModifierKeys.Alt };
        #endregion
        #region 服务设置
        public string SearchEngineName { get; set; } = "Google";
        public bool UsingAndroidUserAgent { get; set; } = true;
        public string LensEngineName { get; set; } = "Google";
        public string TranslateEngineName { get; set; } = "Google";
        public string OcrEngineName { get; set; } = "Windows 内置";
        #endregion
    }
}
