using SelectUnknown.ConfigManagment;
using SelectUnknown.HotKeyMan;
using SelectUnknown.Lens;
using SelectUnknown.LogManagement;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SelectUnknown.Pages
{
    /// <summary>
    /// GeneralPage.xaml 的交互逻辑
    /// </summary>
    public partial class GeneralPage : Page
    {
        public static GeneralPage Instance = new GeneralPage();
        public GeneralPage()
        {
            InitializeComponent();
            IsStartUp.IsChecked = StartupManager.IsStartupEnabled(Main.APP_NAME);
            SilentStart.IsChecked = Config.curConfig.SilentStart;
            AutoCheckUpdate.IsChecked = Config.curConfig.AutoCheckUpdate;
            OldLogDeleteDays.Text = Config.curConfig.OldLogDeleteDays.ToString();
            ScreenshotPath.Text = Config.curConfig.ScreenshotFolderPath;
        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            IsStartUp.IsChecked = StartupManager.IsStartupEnabled(Main.APP_NAME);
            SilentStart.IsChecked = Config.curConfig.SilentStart;
            AutoCheckUpdate.IsChecked = Config.curConfig.AutoCheckUpdate;
            OldLogDeleteDays.Text = Config.curConfig.OldLogDeleteDays.ToString();
            ScreenshotPath.Text = Config.curConfig.ScreenshotFolderPath;
        }
        private void IsStartUp_Checked(object sender, RoutedEventArgs e)
        {
            StartupManager.SetStartup(Main.APP_NAME, true);
        }

        private void IsStartUp_Unchecked(object sender, RoutedEventArgs e)
        {
            StartupManager.SetStartup(Main.APP_NAME, false);
        }

        private void SilentStart_Checked(object sender, RoutedEventArgs e)
        {
            Config.curConfig.SilentStart = true;
            StartupManager.SetStartup(Main.APP_NAME, (bool)IsStartUp.IsChecked);
        }

        private void SilentStart_Unchecked(object sender, RoutedEventArgs e)
        {

            Config.curConfig.SilentStart = false;
            StartupManager.SetStartup(Main.APP_NAME, (bool)IsStartUp.IsChecked);
        }

        private void OldLogDeleteDays_TextChanged(object sender, TextChangedEventArgs e)
        {
            string stringValue = OldLogDeleteDays.Text;
            int value;
            try
            {
                value = int.Parse(stringValue);
            }
            catch
            {
                Main.PopupMessageOnConfigWindow("请输入有效的正整数数字");
                return;
            }
            if(value <= 0)
            {
                Main.PopupMessageOnConfigWindow("请输入有效的正整数数字");
                return;
            }
            Config.curConfig.OldLogDeleteDays = value;
        }

        private void ScreenshotPath_TextChanged(object sender, TextChangedEventArgs e)
        {
            Config.curConfig.ScreenshotFolderPath = ScreenshotPath.Text;
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog folderDialog = new FolderBrowserDialog();

            folderDialog.Description = "";
            folderDialog.ShowNewFolderButton = true;

            // 显示对话框并获取结果
            DialogResult result = folderDialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                // 获取并使用用户选择的路径
                string folderPath = folderDialog.SelectedPath;
                ScreenshotPath.Text = folderPath;
                Config.curConfig.ScreenshotFolderPath = folderPath;
            }
        }

        private void OpenScrshotFolder_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", ScreencatchHelper.GetScreenshotFolderPath());
        }

        private void ScreenshotPath_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!Main.IsPathValid(Config.curConfig.ScreenshotFolderPath, true))
            {
                Main.PopupMessageOnConfigWindow("请输入有效的路径");
                LogHelper.Log($"截图文件夹路径无效({Config.curConfig.ScreenshotFolderPath})(在配置界面触发)", LogLevel.Warn);
            }
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            
            if (System.Windows.MessageBox.Show("确定要重置所有配置吗？", "二次确认", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                ConfigManager.ResetConfig();
                ConfigWindow.Refresh();//刷新一下
                HotKeyHelper.SetStartHotKey();//注册一下
                HotKeyHelper.SetScreenshotHotKey();
                if(Config.curConfig.OcrEngineName != "PaddleOCR-json")
                {
                    OCRHelper.Dispose();//关闭 PaddleOCR-json
                }
            }
        }
        
        private void AutoCheckUpdate_Checked(object sender, RoutedEventArgs e)
        {
            Config.curConfig.AutoCheckUpdate = true;
        }

        private void AutoCheckUpdate_Unchecked(object sender, RoutedEventArgs e)
        {
            Config.curConfig.AutoCheckUpdate = false;
        }

        
    }
}
