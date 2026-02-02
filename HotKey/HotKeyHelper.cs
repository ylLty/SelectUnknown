using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using GlobalHotKey;

namespace SelectUnknown.HotKey
{
    internal class HotKeyHelper
    {
        static HotKeyManager startHotkey = new HotKeyManager();
        static Key startKey;
        static HotKeyManager screenshotHotkey = new HotKeyManager();
        static Key screenshotKey;
        public static void InitHotKey()
        {
            startKey = startHotkey.Register(Key.O, ModifierKeys.Control | ModifierKeys.Alt).Key;
            screenshotKey = screenshotHotkey.Register(Key.F5, ModifierKeys.Control | ModifierKeys.Alt).Key;
        }

        public static void SetStartHotKey()
        {
            
        }
        public static void SetScreenshotHotKey()
        {
            
        }
        private static void HotKeyManager_KeyPressed(object? sender, KeyPressedEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
