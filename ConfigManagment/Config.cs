using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using GlobalHotKey;

namespace SelectUnknown.ConfigManagment
{
    public static class Config
    {
        #region 常规设置
        public static bool SilentStart { get; set; } = false;
        #endregion
        #region 热键设置
        public static Key StartKey { get; set; } = Key.O;
        public static ModifierKeys[] StartModifierKeys { get; set; } = { ModifierKeys.Control };
        public static Key ScreenshotKey { get; set; } = Key.I;
        public static ModifierKeys[] ScreenshotModifierKeys { get; set; } = { ModifierKeys.Control, ModifierKeys.Alt };
        #endregion
    }
}
