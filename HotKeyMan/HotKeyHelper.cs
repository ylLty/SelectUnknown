using GlobalHotKey;
using MaterialDesignThemes.Wpf;
using SelectUnknown.ConfigManagment;
using SelectUnknown.Lens;
using SelectUnknown.LogManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using MessageBox = System.Windows.Forms.MessageBox;

namespace SelectUnknown.HotKeyMan
{
    internal class HotKeyHelper
    {
        static HotKeyManager startHotkey = new HotKeyManager();
        static HotKey startKey;
        static HotKeyManager screenshotHotkey = new HotKeyManager();
        static HotKey screenshotKey;
        public static void InitHotKey()
        {
            SetStartHotKey();
            SetScreenshotHotKey();
        }
        public static int SetStartHotKey()
        {
            // 解析配置中的修饰键数组
            ModifierKeys combined = ModifierKeys.None;
            foreach (var modifier in Config.StartModifierKeys)
            {
                combined |= modifier;
            }
            // 开始注册
            if (startKey != null)
            {
                startHotkey.Unregister(startKey);// 先取消注册旧的
                startHotkey.KeyPressed -= StartHotkeyPressed;
            }
            try
            {
                startKey = startHotkey.Register(Config.StartKey, combined);
            }
            catch (Exception ex)
            {
                LogHelper.Log($"框定即搜热键注册失败{combined} + {Config.StartKey}，可能是因为热键冲突或无效。请检查配置并重试。"+ex, LogLevel.Error);
                MessageBox.Show($"框定即搜热键注册失败{combined} + {Config.StartKey}，可能是因为热键冲突或无效。请检查配置并重试。", "热键注册错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return -1;
            }
            startHotkey.KeyPressed += StartHotkeyPressed;
            LogHelper.Log("框定即搜热键注册成功");
            return 0;
        }
        private static void StartHotkeyPressed(object? sender, KeyPressedEventArgs e)
        {
            LogHelper.Log("框定即搜热键被按下，执行框定即搜操作", LogLevel.Info);
            string selectedWords = Main.GetSelectedText();
            LensHelper.Start(selectedWords);
        }

        public static int SetScreenshotHotKey()
        {
            ConfigManager.ReadConfig();
            // 解析配置中的修饰键数组
            ModifierKeys combined = ModifierKeys.None;
            foreach (var modifier in Config.ScreenshotModifierKeys)
            {
                combined |= modifier;
            }
            // 开始注册
            if(screenshotKey != null)
            {
                screenshotHotkey.Unregister(screenshotKey);// 先取消注册旧的
                screenshotHotkey.KeyPressed -= ScreenshotHotkeyPressed;
            }
            try
            {
                screenshotKey = screenshotHotkey.Register(Config.ScreenshotKey, combined);
            }
            catch (Exception ex)
            {
                LogHelper.Log($"截图热键注册失败{combined} + {Config.ScreenshotKey}，可能是因为热键冲突或无效。请检查配置并重试。"+ex.ToString(), LogLevel.Error);
                MessageBox.Show($"截图热键注册失败{combined} + {Config.ScreenshotKey}，可能是因为热键冲突或无效。请检查配置并重试。", "热键注册错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return -1;
            }
            screenshotHotkey.KeyPressed += ScreenshotHotkeyPressed;
            LogHelper.Log("截图热键注册成功");
            return 0;
        }
        private static void ScreenshotHotkeyPressed(object? sender, KeyPressedEventArgs e)
        {
            LogHelper.Log("截图热键被按下，执行截图操作", LogLevel.Info);
            ScreencatchHelper.Screenshot();
            Main.MousePopup("截图已保存");
        }
    }
}
