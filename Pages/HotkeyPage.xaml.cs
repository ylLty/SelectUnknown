using SelectUnknown.ConfigManagment;
using SelectUnknown.HotKeyMan;
using SelectUnknown.LogManagement;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace SelectUnknown.Pages
{
    /// <summary>
    /// HotkeyPage.xaml 的交互逻辑
    /// </summary>
    public partial class HotkeyPage : Page
    {
        public static HotkeyPage Instance = new HotkeyPage();
        public string StartHotKey { get; set; }
        public string ScreenshotHotKey { get; set; }

        public HotkeyPage()
        {
            InitializeComponent();
        }

        // 设置启动热键按钮的点击事件
        private void SetStartHotKeyButton_Click(object sender, RoutedEventArgs e)
        {
            if (HotKeyHelper.SetStartHotKey() == 0)
            {
                Main.PopupMessageOnConfigWindow("热键注册成功！");
            }
        }

        // 设置截图热键按钮的点击事件
        private void SetScreenshotHotKeyButton_Click(object sender, RoutedEventArgs e)
        {
            if (HotKeyHelper.SetScreenshotHotKey() == 0)
            {
                Main.PopupMessageOnConfigWindow("热键注册成功！");
            }
        }
        static Key curStartKey;
        static List<ModifierKeys> curStartModifierKeys = new List<ModifierKeys>();
        // 捕获启动热键按键
        private void StartKeyInput_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // 获取修饰符键
            ModifierKeys modifiers = Keyboard.Modifiers;
            Key key = e.Key;

            // 更新 Config 中的截图快捷键设置
            Config.curConfig.StartModifierKeys = modifiers.ToString()
                                                     .Split(',')
                                                     .Select(m => (ModifierKeys)Enum.Parse(typeof(ModifierKeys), m.Trim()))
                                                     .ToArray();
            Config.curConfig.StartKey = e.Key;

            // 格式化启动快捷键字符串
            StartHotKey = $"{string.Join("+", Config.curConfig.StartModifierKeys)}+{Config.curConfig.StartKey}";
            StartKeyInput.Text = StartHotKey;
            ConfigManager.SaveConfig();
            //HotKeyHelper.SetStartHotKey();

            e.Handled = true;
        }

        // 捕获截图热键按键
        private void ScreenshotKeyInput_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // 获取修饰符键
            ModifierKeys modifiers = Keyboard.Modifiers;
            Key key = e.Key;

            // 更新 Config 中的截图快捷键设置
            Config.curConfig.ScreenshotModifierKeys = modifiers.ToString()
                                                     .Split(',')
                                                     .Select(m => (ModifierKeys)Enum.Parse(typeof(ModifierKeys), m.Trim()))
                                                     .ToArray();
            Config.curConfig.ScreenshotKey = e.Key;

            // 格式化截图快捷键字符串
            ScreenshotHotKey = $"{string.Join("+", Config.curConfig.ScreenshotModifierKeys)}+{Config.curConfig.ScreenshotKey}";
            ScreenshotKeyInput.Text = ScreenshotHotKey;
            ConfigManager.SaveConfig();
            //HotKeyHelper.SetScreenshotHotKey();

            e.Handled = true;
        }

        // 页面加载时初始化快捷键显示
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // 显示当前的快捷键配置
            StartHotKey = $"{string.Join("+", Config.curConfig.StartModifierKeys)} + {Config.curConfig.StartKey}";
            StartKeyInput.Text = StartHotKey;

            ScreenshotHotKey = $"{string.Join("+", Config.curConfig.ScreenshotModifierKeys)} + {Config.curConfig.ScreenshotKey}";
            ScreenshotKeyInput.Text = ScreenshotHotKey;
        }
    }
}
