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
            InitializeComponent();
            //显示加载窗口
            var loadWindow = new LoadWindow();
            loadWindow.Show();
            //开始加载
            Main.Init();
            //加载完成，关闭加载窗口
            loadWindow.Close();

            this.Closing += ConfigWindow_Closing;
        }

        private void ConfigWindow_Closing(object? sender, CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }

        private void HotkeyButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void GeneralButton_Click(object sender, RoutedEventArgs e)
        {
            mainFrame.Navigate(new GeneralPage());
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            mainFrame.Navigate(new AboutPage());
        }
    }
}
