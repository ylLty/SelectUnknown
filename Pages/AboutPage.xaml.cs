using SelectUnknown.LogManagement;
using System;
using System.Collections.Generic;
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
    /// AboutPage.xaml 的交互逻辑
    /// </summary>
    public partial class AboutPage : Page
    {
        public static readonly AboutPage Instance = new AboutPage();
        public AboutPage()
        {
            InitializeComponent();
            Version.Text = "版本号: " + Main.APP_VERSION;
            Copyright.Text = Main.COPYRIGHT_INFO;
        }

        private void VisitDevSpace_Click(object sender, RoutedEventArgs e)
        {
            LogHelper.Log("用户通过关于页面打开了开发者哔哩哔哩空间", LogLevel.Info);
            Main.OpenUrl("https://space.bilibili.com/474686923");
        }

        private void VisitGithub_Click(object sender, RoutedEventArgs e)
        {
            LogHelper.Log("用户通过关于页面打开了项目GitHub页面", LogLevel.Info);
            Main.OpenUrl("https://github.com/ylLty/SelectUnknown");
        }

        private void Feedback_Click(object sender, RoutedEventArgs e)
        {
            LogHelper.Log("用户通过关于页面打开了问题反馈页面", LogLevel.Info);
            Main.OpenUrl("https://github.com/ylLty/SelectUnknown/issues");
        }

        private void ViewLogs_Click(object sender, RoutedEventArgs e)
        {
            LogHelper.Log("用户通过关于页面打开了日志文件所在目录", LogLevel.Info);
            LogHelper.OpenLogDirectory();
        }

        private void CheckUpdate_Click(object sender, RoutedEventArgs e)
        {

        }

    }
}
