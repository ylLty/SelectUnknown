using SelectUnknown.ConfigManagment;
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
            SilentStart.IsChecked = Config.SilentStart;
            OldLogDeleteDays.Text = Config.OldLogDeleteDays.ToString();
            ScreenshotPath.Text = Config.ScreenshotFolderPath;
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
            Config.SilentStart = true;
            StartupManager.SetStartup(Main.APP_NAME, (bool)IsStartUp.IsChecked);
        }

        private void SilentStart_Unchecked(object sender, RoutedEventArgs e)
        {

            Config.SilentStart = false;
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
            Config.OldLogDeleteDays = value;
        }

        private void ScreenshotPath_TextChanged(object sender, TextChangedEventArgs e)
        {
            Config.ScreenshotFolderPath = ScreenshotPath.Text;
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
                Config.ScreenshotFolderPath = folderPath;
            }
        }

        private void OpenScrshotFolder_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", ScreencatchHelper.GetScreenshotFolderPath());
        }

        private void ScreenshotPath_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!Main.IsPathValid(Config.ScreenshotFolderPath, true))
            {
                Main.PopupMessageOnConfigWindow("请输入有效的路径");
                LogHelper.Log($"截图文件夹路径无效({Config.ScreenshotFolderPath})(在配置界面触发)", LogLevel.Warn);
            }
        }
    }
}
