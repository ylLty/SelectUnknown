using SelectUnknown.ConfigManagment;
using SelectUnknown.HotKeyMan;
using SelectUnknown.Pages;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SelectUnknown
{
    /// <summary>
    /// ConfigWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ConfigWindow : Window
    {
        public ConfigWindow()
        {
            // 显示加载窗口
            var loadWindow = new LoadWindow();
            loadWindow.Show();
            // 开始加载
            Main.Init();
            AddHandler(UIElement.PreviewMouseDownEvent,
                  new MouseButtonEventHandler(GlobalPreviewMouseDown),
                  true);
            // 如果是静默启动模式，不显示配置窗口, 但是启动动画还是会有的
            if (App.silentMode)
            {
                App.silentMode = false;// 重置静默模式标志
                this.Closing += ConfigWindow_Closing;
                this.Close();
                //关闭加载窗口
                loadWindow.Close();
                return;
            }
            InitializeComponent();
            

            //加载完成，关闭加载窗口
            loadWindow.Close();

            this.Closing += ConfigWindow_Closing;
        }
        public static void Refresh()
        {
            GeneralPage.Instance.IsStartUp.IsChecked = StartupManager.IsStartupEnabled(Main.APP_NAME);
            GeneralPage.Instance.SilentStart.IsChecked = Config.curConfig.SilentStart;
            GeneralPage.Instance.AutoCheckUpdate.IsChecked = Config.curConfig.AutoCheckUpdate;
            GeneralPage.Instance.OldLogDeleteDays.Text = Config.curConfig.OldLogDeleteDays.ToString();
            GeneralPage.Instance.ScreenshotPath.Text = Config.curConfig.ScreenshotFolderPath;

            HotkeyPage.Instance.StartHotKey = $"{string.Join("+", Config.curConfig.StartModifierKeys)} + {Config.curConfig.StartKey}";
            HotkeyPage.Instance.StartKeyInput.Text = HotkeyPage.Instance.StartHotKey;
            HotkeyPage.Instance.ScreenshotHotKey = $"{string.Join("+", Config.curConfig.ScreenshotModifierKeys)} + {Config.curConfig.ScreenshotKey}";
            HotkeyPage.Instance.ScreenshotKeyInput.Text = HotkeyPage.Instance.ScreenshotHotKey;

            ServicePage.Instance.SearchEngineSelect.Text = Config.curConfig.SearchEngineName;
            ServicePage.Instance.SearchEngineSelect.SelectedItem = Config.curConfig.SearchEngineName;
            ServicePage.Instance.UsingAndroidUserAgentCheck.IsChecked = Config.curConfig.UsingAndroidUserAgent;
            ServicePage.Instance.LensEngineSelect.Text = Config.curConfig.LensEngineName;
            ServicePage.Instance.TranslateEngineSelect.Text = Config.curConfig.TranslateEngineName;
            ServicePage.Instance.OcrEngineSelect.Text = Config.curConfig.OcrEngineName;
        }
        /// <summary>
        /// 监听窗口鼠标点击事件用于自动保存
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void GlobalPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Task.Run(async () =>
            {
                Thread.Sleep(200);// 等待配置项变化完成
                ConfigManagment.ConfigManager.SaveConfig();
                ConfigManagment.ConfigManager.ReadConfig();
                //HotKeyHelper.SetScreenshotHotKey();
            });
            Refresh();
        }

        private void ConfigWindow_Closing(object? sender, CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }

        private void HotkeyButton_Click(object sender, RoutedEventArgs e)
        {
            mainFrame.Navigate(HotkeyPage.Instance);
        }

        private void GeneralButton_Click(object sender, RoutedEventArgs e)
        {
            mainFrame.Navigate(GeneralPage.Instance);
        }

       
        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            mainFrame.Navigate(AboutPage.Instance);
        }

        public void PopupMsg(string msg)
        {
            MainSnackbar.MessageQueue?.Enqueue(msg);
        }

        private void ServiceButton_Click(object sender, RoutedEventArgs e)
        {
            mainFrame.Navigate(ServicePage.Instance);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (Config.curConfig.AutoCheckUpdate)
                Task.Run(() => Main.CheckUpdate());
        }
    }
}
